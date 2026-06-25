using FluentValidation;
using System.Text.Json;

namespace Jomla.Application.Common.Extensions
{
    public static class FluentValidationExtensions
    {
        public static IRuleBuilderOptions<T, string?> IsValidJson<T>(this IRuleBuilder<T, string?> ruleBuilder)
        {
            return ruleBuilder.Must(jsonString =>
            {
                if (string.IsNullOrWhiteSpace(jsonString))
                    return false;

                try
                {
                    using (JsonDocument.Parse(jsonString)) { }
                    return true;
                }
                catch (JsonException)
                {
                    return false;
                }
            }).WithMessage("{PropertyName} contains an invalid JSON format.");
        }
    }
}
