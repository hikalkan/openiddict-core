﻿/*
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0)
 * See https://github.com/openiddict/openiddict-core for more information concerning
 * the license and the contributors participating to this project.
 */

using System.Collections.Immutable;
using System.Text.Json;

namespace OpenIddict.Client;

public static partial class OpenIddictClientHandlers
{
    public static class Exchange
    {
        public static ImmutableArray<OpenIddictClientHandlerDescriptor> DefaultHandlers { get; } = ImmutableArray.Create(
            /*
             * Token response handling:
             */
            HandleErrorResponse<HandleTokenResponseContext>.Descriptor,
            ValidateWellKnownParameters.Descriptor);

        /// <summary>
        /// Contains the logic responsible of validating the well-known parameters contained in the token response.
        /// </summary>
        public class ValidateWellKnownParameters : IOpenIddictClientHandler<HandleTokenResponseContext>
        {
            /// <summary>
            /// Gets the default descriptor definition assigned to this handler.
            /// </summary>
            public static OpenIddictClientHandlerDescriptor Descriptor { get; }
                = OpenIddictClientHandlerDescriptor.CreateBuilder<HandleTokenResponseContext>()
                    .UseSingletonHandler<ValidateWellKnownParameters>()
                    .SetOrder(int.MinValue + 100_000)
                    .SetType(OpenIddictClientHandlerType.BuiltIn)
                    .Build();

            /// <inheritdoc/>
            public ValueTask HandleAsync(HandleTokenResponseContext context)
            {
                if (context is null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                foreach (var parameter in context.Response.GetParameters())
                {
                    if (ValidateParameterType(parameter.Key, parameter.Value.Value))
                    {
                        continue;
                    }

                    context.Reject(
                        error: Errors.ServerError,
                        description: SR.FormatID2107(parameter.Key),
                        uri: SR.FormatID8000(SR.ID2107));

                    return default;
                }

                return default;

                static bool ValidateParameterType(string name, object? value) => name switch
                {
                    // The 'expires_in' parameter MUST be formatted as a numeric date value.
                    Parameters.ExpiresIn => value is long or JsonElement { ValueKind: JsonValueKind.Number },

                    // The 'access_token', 'id_token' and 'refresh_token' parameters MUST be formatted as unique strings.
                    Parameters.AccessToken or Parameters.IdToken or Parameters.RefreshToken
                        => value is string or JsonElement { ValueKind: JsonValueKind.String },

                    // Parameters that are not in the well-known list can be of any type.
                    _ => true
                };
            }
        }
    }
}
