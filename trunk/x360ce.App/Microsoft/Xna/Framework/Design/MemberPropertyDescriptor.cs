namespace Microsoft.Xna.Framework.Design
{
    using System;
    using System.ComponentModel;
    using System.Reflection;

    internal abstract class MemberPropertyDescriptor : PropertyDescriptor
    {
        private MemberInfo _member;

        public MemberPropertyDescriptor(MemberInfo member) : base(member.Name, (Attribute[]) member.GetCustomAttributes(typeof(Attribute), true))
        {
            this._member = member;
        }

        public override bool CanResetValue(object component)
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            MemberPropertyDescriptor descriptor = obj as MemberPropertyDescriptor;
            return ((descriptor != null) && descriptor._member.Equals(this._member));
        }

        public override int GetHashCode()
        {
            return this._member.GetHashCode();
        }

        public override void ResetValue(object component)
        {
        }

        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }

        public override Type ComponentType
        {
            get
            {
                return this._member.DeclaringType;
            }
        }

        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }
    }
}

