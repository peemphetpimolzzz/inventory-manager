namespace InventoryManager.Api.Common;

public class NotFoundException(string message) : Exception(message);

public class BusinessRuleException(string message) : Exception(message);
