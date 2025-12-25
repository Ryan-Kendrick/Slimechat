namespace Exceptions;

public class ResourceNotFoundException(string message) : Exception(message);

public class BadRequestException(string message) : Exception(message);

public class UnauthorizedException(string message) : Exception(message);