namespace Microsoft.Xna.Framework.Design
{
    using System;
    using System.Reflection;

    internal class FieldPropertyDescriptor : MemberPropertyDescriptor
    {
        private FieldInfo _field;

        public FieldPropertyDescriptor(FieldInfo field) : base(field)
        {
            this._field = field;
        }

        public override object GetValue(object component)
        {
            return this._field.GetValue(component);
        }

        public override void SetValue(object component, object value)
        {
            this._field.SetValue(component, value);
            this.OnValueChanged(component, EventArgs.Empty);
        }

        public override Type PropertyType
        {
            get
            {
                return this._field.FieldType;
            }
        }
    }
}

