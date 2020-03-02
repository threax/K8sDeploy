using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Rest
{
    class ThreaxShim { }
}

namespace k8s.Models
{
    public enum ValidationRules
    {
        CannotBeNull
    }

    public class ValidationException : Exception
    {
        public ValidationRules Rules { get; private set; }

        public ValidationException(ValidationRules rules, String message)
        {
            this.Rules = rules;
        }
    }
}
