using System;

namespace Jomla.Application.Common.Exceptions
{
    public class BadRequestException(string message) : Exception(message);
}
