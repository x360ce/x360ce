namespace Microsoft.Xna.Framework.Design
{
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design.Serialization;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Reflection;

    public class Vector2Converter : MathTypeConverter
    {
        public Vector2Converter()
        {
            Type type = typeof(Vector2);
            base.propertyDescriptions = new PropertyDescriptorCollection(new PropertyDescriptor[] { new FieldPropertyDescriptor(type.GetField("X")), new FieldPropertyDescriptor(type.GetField("Y")) }).Sort(new string[] { "X", "Y" });
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            float[] numArray = MathTypeConverter.ConvertToValues<float>(context, culture, value, 2, new string[] { "X", "Y" });
            if (numArray != null)
            {
                return new Vector2(numArray[0], numArray[1]);
            }
            return base.ConvertFrom(context, culture, value);
        }

        [SuppressMessage("Microsoft.Performance", "CA1800")]
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException("destinationType");
            }
            if ((destinationType == typeof(string)) && (value is Vector2))
            {
                Vector2 vector2 = (Vector2) value;
                return MathTypeConverter.ConvertFromValues<float>(context, culture, new float[] { vector2.X, vector2.Y });
            }
            if ((destinationType == typeof(InstanceDescriptor)) && (value is Vector2))
            {
                Vector2 vector = (Vector2) value;
                ConstructorInfo constructor = typeof(Vector2).GetConstructor(new Type[] { typeof(float), typeof(float) });
                if (constructor != null)
                {
                    return new InstanceDescriptor(constructor, new object[] { vector.X, vector.Y });
                }
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (propertyValues == null)
            {
                throw new ArgumentNullException("propertyValues", FrameworkResources.NullNotAllowed);
            }
            return new Vector2((float) propertyValues["X"], (float) propertyValues["Y"]);
        }
    }
}

