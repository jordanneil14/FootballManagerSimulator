using System.Reflection;
using FootballManagerSimulator.Maui.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace FootballManagerSimulator.Maui.Services;

/// <summary>
/// Serializer settings for save files. Only <see cref="StateModel.Clubs"/> holds full clubs; every
/// other club (fixtures, competitions) is written as just its Id and Name and relinked to
/// <see cref="StateModel.Clubs"/> on load. Without this each fixture repeated the whole club,
/// lineup included, making saves roughly ten times larger.
/// </summary>
public static class SaveSerializer
{
    private static readonly JsonSerializerSettings Settings = new()
    {
        TypeNameHandling = TypeNameHandling.Auto,
        ContractResolver = new SaveContractResolver()
    };

    public static JsonSerializer Create() => JsonSerializer.Create(Settings);

    private sealed class SaveContractResolver : DefaultContractResolver
    {
        private static readonly ClubReferenceConverter ClubReference = new();

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            if (member.DeclaringType == typeof(StateModel))
                return property;

            if (property.PropertyType == typeof(ClubModel))
                property.Converter = ClubReference;
            else if (property.PropertyType == typeof(List<ClubModel>))
                property.ItemConverter = ClubReference;

            return property;
        }
    }

    private sealed class ClubReferenceConverter : JsonConverter<ClubModel>
    {
        public override void WriteJson(JsonWriter writer, ClubModel? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName(nameof(ClubModel.Id));
            writer.WriteValue(value.Id);
            writer.WritePropertyName(nameof(ClubModel.Name));
            writer.WriteValue(value.Name);
            writer.WriteEndObject();
        }

        // Older saves hold the full club here; only the Id and Name are needed to relink it.
        public override ClubModel? ReadJson(JsonReader reader, Type objectType, ClubModel? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;

            var club = JObject.Load(reader);
            return new ClubModel
            {
                Id = club.Value<int>(nameof(ClubModel.Id)),
                Name = club.Value<string>(nameof(ClubModel.Name)) ?? ""
            };
        }
    }
}
