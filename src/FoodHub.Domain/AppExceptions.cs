namespace FoodHub.Domain;

// Lỗi nghiệp vụ cần mã HTTP riêng. Cố ý KHÔNG kế thừa DomainException (vốn ứng với 400).
public class UnauthorizedException(string message) : Exception(message);   // 401
public class ForbiddenException(string message) : Exception(message);      // 403
public class NotFoundException(string message) : Exception(message);       // 404
public class ConflictException(string message) : Exception(message);       // 409
