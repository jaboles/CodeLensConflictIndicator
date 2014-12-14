//-----------------------------------------------------------------------
// <copyright company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace CodeLens.ConflictIndicator
{
    /// <summary>
    /// A class that provides helper methods for easily calling members of a nonpublic class that is being reflected.
    /// </summary>
    public class ReflectionProxy
    {
        /// <summary>
        /// Creates a new ReflectionProxy that will reflect a type in an assembly.
        /// </summary>
        /// <param name="assembly">The assembly containing the type to reflect.</param>
        /// <param name="fullyQualifiedTypeName">The type to reflect.</param>
        public ReflectionProxy(Assembly assembly, string fullyQualifiedTypeName)
        {
            this.Assembly = assembly;
            this.Type = assembly.GetType(fullyQualifiedTypeName, true, false);
        }

        /// <summary>
        /// Creates a new ReflectionProxy that will reflect an object of a non-public type.
        /// </summary>
        /// <param name="privateObject">The non-public object.</param>
        public ReflectionProxy(object privateObject)
        {
            if (privateObject != null)
            {
                this.Type = privateObject.GetType();
                this.Assembly = this.Type.Assembly;
                this.PrivateObject = privateObject;
            }
        }

        /// <summary>
        /// Gets the assembly containing the reflected type.
        /// </summary>
        public Assembly Assembly { get; private set; }

        /// <summary>
        /// Gets the reflected type.
        /// </summary>
        public Type Type { get; private set; }

        /// <summary>
        /// Gets the reflected object.
        /// </summary>
        public object PrivateObject { get; private set; }

        public void AddEventHandler(string eventName, DynamicMethod eventHandler, BindingFlags bindingFlags = BindingFlags.Default, Type overrideType = null)
        {
            if (overrideType == null)
            {
                overrideType = this.Type;
            }

            EventInfo ei = overrideType.GetEvent(eventName, bindingFlags);
            Type delegateType = ei.EventHandlerType;
            Delegate d = eventHandler.CreateDelegate(delegateType);
            MethodInfo addEventHandlerMethod = ei.GetAddMethod(true);
            addEventHandlerMethod.Invoke(this.PrivateObject, new[] { d });
        }

        public void RemoveEventHandler(string eventName, DynamicMethod eventHandler, BindingFlags bindingFlags = BindingFlags.Default, Type overrideType = null)
        {
            if (overrideType == null)
            {
                overrideType = this.Type;
            }

            EventInfo ei = overrideType.GetEvent(eventName, bindingFlags);
            Type delegateType = ei.EventHandlerType;
            Delegate d = eventHandler.CreateDelegate(delegateType);
            MethodInfo removeEventHandlerMethod = ei.GetRemoveMethod(true);
            removeEventHandlerMethod.Invoke(this.PrivateObject, new[] { d });
        }

        public Type[] GetHandlerDelegateParameterTypes(string eventName, BindingFlags bindingFlags = BindingFlags.Default, Type overrideType = null)
        {
            if (overrideType == null)
            {
                overrideType = this.Type;
            }

            EventInfo ei = overrideType.GetEvent(eventName, bindingFlags);
            Type delegateType = ei.EventHandlerType;
            MethodInfo invokeMethod = delegateType.GetMethod("Invoke");
            Type[] parameterTypes = invokeMethod.GetParameters().Select(parameterInfo => parameterInfo.ParameterType).ToArray();
            return parameterTypes;
        }

        /// <summary>
        /// Invokes a constructor.
        /// </summary>
        /// <param name="args">The arguments to pass to the constructor.</param>
        protected void InvokeConstructor(object[] args)
        {
            this.InvokeConstructor(BindingFlags.Default, args);
        }

        protected void InvokeConstructor(BindingFlags bindingFlags, object[] args)
        {
            Type[] types = new Type[] { };
            if (args != null)
            {
                types = args.Select(arg => arg.GetType()).ToArray();
            }

            ConstructorInfo ci = this.Type.GetConstructor(bindingFlags, null, types, null);

            if (ci != null)
            {
                this.PrivateObject = ci.Invoke(bindingFlags, null, args, null);
            }
            else
            {
                // No constructor!
                this.PrivateObject = Activator.CreateInstance(this.Type);
            }
        }

        protected object InvokeMethod(string name, object[] args = null, ParameterModifier[] modifiers = null)
        {
            return this.InvokeMethod(BindingFlags.Default, name, args, null, modifiers);
        }

        protected object InvokeMethod(BindingFlags bindingFlags, string name, object[] args = null, Type[] types = null, ParameterModifier[] modifiers = null, Type overrideType = null)
        {
            if (overrideType == null)
            {
                overrideType = this.Type;
            }

            if (types == null && args != null)
            {
                types = args.Select(arg => arg.GetType()).ToArray();
            }

            MethodInfo mi = null;
            object returned = null;
            if (args != null)
            {
                mi = overrideType.GetMethod(name, bindingFlags, null, types, modifiers);
            }
            else
            {
                mi = overrideType.GetMethod(name, bindingFlags);
            }

            try
            {
                returned = mi.Invoke(this.PrivateObject, bindingFlags, null, args, null);
            }
            catch (TargetInvocationException ex)
            {
                // using Invoke() will wrap any exception that occurs in a TargetInvocationException. Unwrap and rethrow the inner exception.
                throw ex.InnerException;
            }

            return returned;
        }

        protected object InvokeGetProperty(string name, BindingFlags bindingFlags = BindingFlags.Default, Type overrideType = null)
        {
            if (overrideType == null)
            {
                overrideType = this.Type;
            }

            PropertyInfo pi = overrideType.GetProperty(name, bindingFlags | BindingFlags.GetProperty);
            MethodInfo mi = pi.GetMethod;
            object returned = mi.Invoke(this.PrivateObject, bindingFlags | BindingFlags.GetProperty, null, null, null);
            return returned;
        }

        protected void InvokeSetProperty(string name, object value, BindingFlags bindingFlags = BindingFlags.Default, Type overrideType = null)
        {
            if (overrideType == null)
            {
                overrideType = this.Type;
            }

            PropertyInfo pi = overrideType.GetProperty(name, bindingFlags | BindingFlags.SetProperty);
            MethodInfo mi = pi.SetMethod;
            mi.Invoke(this.PrivateObject, bindingFlags | BindingFlags.SetProperty, null, new object[] { value }, null);
        }

        protected object ConvertEnum(int value, Type type)
        {
            object convertedEnumValue = Convert.ChangeType(value, type);
            return convertedEnumValue;
        }

        protected object GetField(string name, Type overrideType = null)
        {
            if (overrideType == null)
            {
                overrideType = this.Type;
            }

            FieldInfo fi = overrideType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object value = fi.GetValue(this.PrivateObject);
            return value;
        }
    }
}
