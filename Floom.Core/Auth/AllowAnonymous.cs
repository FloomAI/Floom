using System;

namespace Floom.Auth
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true)]
    public class AllowAnonymousAttribute : Attribute
    {
    }
}
