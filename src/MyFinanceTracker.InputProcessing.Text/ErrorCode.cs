namespace MyFinanceTracker.InputProcessing.Text;

public static class ErrorCode
{
    public static class Syntax
    {
        public const string EmptyInput = nameof(EmptyInput);
        public const string InvalidFormat = nameof(InvalidFormat);
        public const string InvalidAmount = nameof(InvalidAmount);
        public const string InvalidDateFormat = nameof(InvalidDateFormat); 
    }
}