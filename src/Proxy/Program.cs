using Amazon.SQS;
using Amazon.SQS.Model;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonSQS>();

var app = builder.Build();

var queueUrl = app.Configuration["SQS_QUEUE_URL"];

app.MapGet("/2018-06-01/runtime/invocation/next", async (HttpContext context, IAmazonSQS sqsClient, CancellationToken cancellationToken) =>
{
    while (!cancellationToken.IsCancellationRequested)
    {
        var messages = await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
        {
            QueueUrl = queueUrl,
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = 20,
            MessageAttributeNames = new List<string> {"All"}
        }, cancellationToken);

        if (messages.Messages.Count > 0)
        {
            context.Response.Headers["Lambda-Runtime-Aws-Request-Id"] = messages.Messages.Single().ReceiptHandle;
            return new { Records = messages.Messages };
        }
    }

    throw new TaskCanceledException();
});

app.MapPost("/2018-06-01/runtime/invocation/{receiptHandle}/response", async (string receiptHandle, IAmazonSQS sqsClient) =>
{
    if (receiptHandle != "none")
    {
        await sqsClient.DeleteMessageAsync(new DeleteMessageRequest
        {
            QueueUrl = queueUrl,
            ReceiptHandle = receiptHandle
        });
    }
    return Results.Accepted(value: new {status="ok"} );
});

app.MapPost("/2018-06-01/runtime/invocation/{awsRequestId}/error", (string awsRequestId) => Results.Accepted(value: new { status="ok" } ));
    
app.Run();