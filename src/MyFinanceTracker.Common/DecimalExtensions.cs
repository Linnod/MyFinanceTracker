namespace MyFinanceTracker.Common;

public static class DecimalExtensions
{
    public static int GetScale(this decimal value)
    {
        int[] bits = decimal.GetBits(value);
        
        return (bits[3] >> 16) & 0x7F;
    }
}