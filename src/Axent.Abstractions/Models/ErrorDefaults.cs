using System.Net;

namespace Axent.Abstractions.Models;

public static class ErrorDefaults
{
    public static class Generic
    {
        public static Error NotFound() =>
            new($"{nameof(Generic)}.{nameof(NotFound)}", HttpStatusCode.NotFound);

        public static Error Forbidden() =>
            new($"{nameof(Generic)}.{nameof(Forbidden)}", HttpStatusCode.Forbidden);

        public static Error Unauthorized() =>
            new($"{nameof(Generic)}.{nameof(Unauthorized)}", HttpStatusCode.Unauthorized);

        public static Error Conflict() =>
            new($"{nameof(Generic)}.{nameof(Conflict)}", HttpStatusCode.Conflict);

        public static Error UnprocessableEntity() =>
            new($"{nameof(Generic)}.{nameof(UnprocessableEntity)}", HttpStatusCode.UnprocessableEntity);

        public static Error BadRequest() =>
            new($"{nameof(Generic)}.{nameof(BadRequest)}", HttpStatusCode.BadRequest);

        public static Error InternalServerError() =>
            new($"{nameof(Generic)}.{nameof(InternalServerError)}", HttpStatusCode.InternalServerError);

        public static Error ValidationFailure() => new($"{nameof(Generic)}.{nameof(ValidationFailure)}", HttpStatusCode.BadRequest);

    }
}
