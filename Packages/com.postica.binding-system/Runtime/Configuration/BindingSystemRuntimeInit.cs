using Postica.BindingSystem.Converters;
using Postica.BindingSystem.Modifiers;
using Postica.BindingSystem.Tweening;
using UnityEngine;

namespace Postica.BindingSystem
{
    internal class BindingSystemRuntimeInit
    {
        private static bool _initialized;

        static BindingSystemRuntimeInit() => Init();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        internal static void Init()
        {
            if (_initialized) { return; }
            _initialized = true;

            RegisterConverters();
            RegisterModifiers();
            RegisterProviders();
        }

        private static void RegisterConverters()
        {
            // Register primitives conversions
            ConvertersFactory.Register<string, byte>(byte.Parse, false);
            ConvertersFactory.Register<string, short>(short.Parse, false);
            ConvertersFactory.Register<string, int>(int.Parse, false);
            ConvertersFactory.Register<string, long>(long.Parse, false);
            ConvertersFactory.Register<string, uint>(uint.Parse, false);

            ConvertersFactory.Register<byte, float>(v => v);
            ConvertersFactory.Register<short, float>(v => v);
            ConvertersFactory.Register<int, float>(v => v);
            ConvertersFactory.Register<long, float>(v => v);

            ConvertersFactory.Register<byte, double>(v => v);
            ConvertersFactory.Register<short, double>(v => v);
            ConvertersFactory.Register<int, double>(v => v);
            ConvertersFactory.Register<long, double>(v => v);

            ConvertersFactory.Register<float, double>(v => v);
            ConvertersFactory.Register<double, float>(v => (float)v);

            ConvertersFactory.Register<object, string>(o => o?.ToString());

            // Special Converters
            ConvertersFactory.Register<Vector2, Vector4>(v => v);
            ConvertersFactory.Register<Vector2, Vector3>(v => v);
            ConvertersFactory.Register<Vector3, Vector2>(v => v);
            ConvertersFactory.Register<Vector3, Vector4>(v => v);
            ConvertersFactory.Register<Vector4, Vector2>(v => v);
            ConvertersFactory.Register<Vector4, Vector3>(v => v);
            ConvertersFactory.Register<Vector4, Color>(v => v);
            ConvertersFactory.Register<Color, Vector4>(v => v);

            // Register Templates
            ConvertersFactory.RegisterTemplate<StringToFloatConverter>();
            ConvertersFactory.RegisterTemplate<StringToDoubleConverter>();
            ConvertersFactory.RegisterTemplate<StringToDecimalConverter>();
            ConvertersFactory.RegisterTemplate<FormatStringConverter>();
            ConvertersFactory.RegisterTemplate<NumericToBoolConverter>();
            ConvertersFactory.RegisterTemplate<GradientToColorConverter>();
            ConvertersFactory.RegisterTemplate<StringToColorConverter>();
            ConvertersFactory.RegisterTemplate<NumericToVector2Converter>();
            ConvertersFactory.RegisterTemplate<NumericToVector3Converter>();
            ConvertersFactory.RegisterTemplate<NumericToVector4Converter>();
            ConvertersFactory.RegisterTemplate<NumericToVector3IntConverter>();
            ConvertersFactory.RegisterTemplate<NumericToVector2IntConverter>();
            ConvertersFactory.RegisterTemplate<NumericToColorConverter2>();
            ConvertersFactory.RegisterTemplate<DecimalToIntegerConverter>();
            
            // Special Converters
            ConvertersFactory.RegisterTemplate<StringBuilderToStringConverter>();
            ConvertersFactory.RegisterTemplate<CharArrayToStringConverter>();
            ConvertersFactory.RegisterTemplate<StringToCharArrayConverter>();

            // Register Default Types
            EnumConverters.RegisterDefaultTypes();
            BooleanConverters.RegisterDefaultTypes();
            FormatStringConverters.RegisterDefaultTypes();
            ListArrayConverters.RegisterDefaultTypes();
            UnityEventConverters.RegisterDefaultTypes();
        }

        private static void RegisterModifiers()
        {
            ModifiersFactory.Register<AbsoluteValueModifier>();
            ModifiersFactory.Register<NormalizeValueModifier>();
            ModifiersFactory.Register<ClampValueModifier>();
            ModifiersFactory.Register<ModulusValueModifier>();
            ModifiersFactory.Register<OperationModifier>();
            ModifiersFactory.Register<StringFormatModifier>();
            ModifiersFactory.Register<StringConcatModifier>();
            ModifiersFactory.Register<StringCasingModifier>();
            ModifiersFactory.Register<SubstringModifier>();
            ModifiersFactory.Register<InvertBoolModifier>();
            ModifiersFactory.Register<LogicOperationModifier>();
            ModifiersFactory.Register<TintColorModifier>();
            ModifiersFactory.Register<AddColorModifier>();
            ModifiersFactory.Register<AdjustColorModifier>();
            
            // Math Expression Modifiers Family
            ModifiersFactory.Register<MathExpressionModifier>();
            ModifiersFactory.Register<ColorMathExpressionModifier>();
            ModifiersFactory.Register<Vector4MathExpressionModifier>();
            ModifiersFactory.Register<Vector3MathExpressionModifier>();
            ModifiersFactory.Register<Vector2MathExpressionModifier>();
            
            // Bool Expression Modifiers Family
            ModifiersFactory.Register<BoolExpressionModifier>();
            
            // Register other known modifiers
            ModifiersFactory.Register<ColorSwizzleModifier>();
            
            // These RegisterAll methods register multiple modifiers at once
            LinkModifiers.RegisterAll();
            SetValueModifiers.RegisterAll();
            DelayValueModifiers.RegisterAll();
            NullModifiers.RegisterAll();
            AddVectorModifiers.RegisterAll();
            ScaleVectorModifiers.RegisterAll();
            TweenSystem.RegisterKnownEaseModifiers();
            TweenSystem.RegisterKnownDampedTrackerModifiers();
        }

        private static void RegisterProviders()
        {
            AccessorsFactory.RegisterAccessorProvider(new Accessors.MaterialsAccessorProvider());
        }
    }
}
