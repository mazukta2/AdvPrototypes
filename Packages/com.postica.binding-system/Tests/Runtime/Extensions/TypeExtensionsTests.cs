using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Postica.Common;
using UnityEngine;
using UnityEngine.TestTools;

using Object = UnityEngine.Object;

namespace Postica.BindingSystem {

    internal class SimpleClass { }

    internal class CompoundClass
    {
        public class InnerClass { }
    }

    internal class GenericClass<S, T>
    {
        public class InnerClass { }
        public class InnerGenericClass<M> { }
        public class InnerGenericHardClass<SInner> { }
    }

    public class TypeExtensionsTests
    {
        [Test]
        public void Get_Signature_For_Simple_Type_Passes()
        {
            var signature = typeof(SimpleClass).ToSignatureString();
            Assert.AreEqual("Postica.BindingSystem.SimpleClass", signature);
        }

        [Test]
        public void Get_Signature_For_Simple_Type_WithoutNamespace_Passes()
        {
            var signature = typeof(SimpleClass).ToSignatureString(withNamespace: false);
            Assert.AreEqual("SimpleClass", signature);
        }

        [Test]
        public void Get_Signature_For_Inner_Type_Passes()
        {
            var signature = typeof(CompoundClass.InnerClass).ToSignatureString();
            Assert.AreEqual("Postica.BindingSystem.CompoundClass.InnerClass", signature);
        }

        [Test]
        public void Get_Signature_For_Generic_Type_Passes()
        {
            var signature = typeof(GenericClass<float, string>).ToSignatureString();
            Assert.AreEqual("Postica.BindingSystem.GenericClass<System.Single, System.String>", signature);
        }

        [Test]
        public void Get_Signature_For_SimpleInner_In_Generic_Type_Passes()
        {
            var signature = typeof(GenericClass<float, string>.InnerClass).ToSignatureString();
            Assert.AreEqual("Postica.BindingSystem.GenericClass<System.Single, System.String>.InnerClass", signature);
        }

        [Test]
        public void Get_Signature_For_GenericInner_In_Generic_Type_Passes()
        {
            var signature = typeof(GenericClass<float, string>.InnerGenericClass<object>).ToSignatureString();
            Assert.AreEqual("Postica.BindingSystem.GenericClass<System.Single, System.String>.InnerGenericClass<System.Object>", signature);
        }
    }
}