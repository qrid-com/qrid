namespace Qrid.Common.Server.Validation.Constant
{
    public interface IValidationAttribute
    {
        bool IsValid(string Value);
    }
}
