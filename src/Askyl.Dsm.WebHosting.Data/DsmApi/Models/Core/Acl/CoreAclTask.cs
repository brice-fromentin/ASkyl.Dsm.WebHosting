using System.Text.Json.Serialization;

namespace Askyl.Dsm.WebHosting.Data.DsmApi.Models.Core.Acl;

public record CoreAclTask
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; init; } = "";
}
