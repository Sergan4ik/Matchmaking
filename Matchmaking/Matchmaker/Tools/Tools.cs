using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Matchmaking.Matchmaker;

public static class Tools
{
    public static int LowerBound<T>(this IList<T> list, T value) where T : IComparable<T>
    {
        int low = 0;
        int high = list.Count - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (list[mid].CompareTo(value) < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }
    public static int LowerBound<T,T1>(this IList<T> list, T1 value, Func<T,T1,int> comparer)
    {
        int low = 0;
        int high = list.Count - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (comparer(list[mid], value) < 0)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return low;
    }
    
    public interface IJsonSerializable
    {
        string WriteToJson();
        void ReadFromJson(string json);
    }
    
    //should be tested
    public class ZergRushJsonConverter : JsonConverter<IJsonSerializable>
    {
        public override void Write(Utf8JsonWriter w, IJsonSerializable value, JsonSerializerOptions _)
        {
            var raw = value.WriteToJson();
            using var doc = JsonDocument.Parse(raw);
            doc.RootElement.WriteTo(w);
        }

        public override IJsonSerializable Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o)
            => throw new NotImplementedException(); 
    }
    
    public class FieldsSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext ctx)
        {
            foreach (var f in ctx.Type.GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var fSchema = ctx.SchemaGenerator.GenerateSchema(f.FieldType, ctx.SchemaRepository);
                schema.Properties[f.Name] = fSchema;
            }
        }
    }
}