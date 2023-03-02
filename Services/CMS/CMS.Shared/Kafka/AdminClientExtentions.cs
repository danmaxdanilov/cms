using Confluent.Kafka;
using Confluent.Kafka.Admin;

namespace CMS.Shared.Kafka;

public static class AdminClientExtentions
{
    public static async Task<(bool IsSuccess, string Message)> TryCreateKafkaTopicAsync(
        this IAdminClient adminClient,
        string topicName,
        int numPartitions = 1)
    {
        var topicSpecification = new []
        {
            new TopicSpecification
            {
                Name = topicName,
                NumPartitions = numPartitions
            }
        };
        
        
        try
        {
            await adminClient.CreateTopicsAsync(topicSpecification).ConfigureAwait(false);
        }
        catch (CreateTopicsException e)
        {
            return (false,
                string.Format("Topic {0} creation error: {1}", topicName, e.Message));
        }

        return (true,
            string.Format("Topic {0} created successfully", topicName));
    }
}