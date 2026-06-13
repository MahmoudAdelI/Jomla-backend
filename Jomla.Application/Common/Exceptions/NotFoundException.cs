namespace Jomla.Application.Common.Exceptions
{
    public class NotFoundException(string name, object id)
    : Exception($"the {name} with id: {id} was not found!");
}
