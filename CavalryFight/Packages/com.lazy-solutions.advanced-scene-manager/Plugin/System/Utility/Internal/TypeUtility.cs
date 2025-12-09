using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;

namespace AdvancedSceneManager.Editor.UI
{

    /// <summary>Contains utility functions for working with types.</summary>
    public static class TypeUtility
    {

#if UNITY_EDITOR
        internal static bool IsViewModel(this Type type) =>
            typeof(ViewModel).IsAssignableFrom(type);

        internal static bool IsPopup(this Type type) =>
            type.IsViewModel() && type.IsType<IPopup>();

        internal static bool IsSettingsPage(this Type type)
        {
            if (!type.IsViewModel())
                return false;

            try
            {
                return type.IsType<ISettingsPage>() || (type.GetCustomAttribute<ASMWindowElementAttribute>()?.location == ElementLocation.Settings);
            }
            catch
            {
                return false;
            }
        }
#endif

        /// <summary>Determines whether the specified <see cref="MemberInfo"/> represents a static member.</summary>
        public static bool IsStatic(this MemberInfo member) => member switch
        {
            MethodInfo m => m.IsStatic,
            PropertyInfo p => p.GetGetMethod(true)?.IsStatic ?? false,
            FieldInfo f => f.IsStatic,
            _ => false
        };

        /// <summary>Gets the return or value type of the specified <see cref="MemberInfo"/>.</summary>
        public static Type ReturnType(this MemberInfo member) => member switch
        {
            MethodInfo m => m.ReturnType,
            PropertyInfo p => p.PropertyType,
            FieldInfo f => f.FieldType,
            Type t => t,
            EventInfo e => e.EventHandlerType,
            _ => null
        };

        /// <summary>Determines whether the specified <see cref="MemberInfo"/> represents a method.</summary>
        public static bool IsMethod(this MemberInfo member) => member is MethodInfo;

        /// <summary>Determines whether the specified <see cref="MemberInfo"/> represents a method returning <typeparamref name="T"/>.</summary>
        public static bool IsMethodAndReturns<T>(this MemberInfo member) => member is MethodInfo method && typeof(T).IsAssignableFrom(method.ReturnType);

        /// <summary>Determines whether the specified <see cref="MemberInfo"/> represents a constructor.</summary>
        public static bool IsConstructor(this MemberInfo member) => member is ConstructorInfo;

        /// <summary>Determines whether the specified <see cref="MemberInfo"/> represents a type.</summary>
        public static bool IsType(this MemberInfo member) => member is Type;

        /// <summary>Determines whether the specified <see cref="MemberInfo"/> represents a type assignable to <typeparamref name="T"/>.</summary>
        public static bool IsType<T>(this MemberInfo member) => typeof(T).IsAssignableFrom(member as TypeInfo);

        /// <summary>Determines whether the specified <see cref="MemberInfo"/> represents a field.</summary>
        public static bool IsField(this MemberInfo member) => member is FieldInfo;

        /// <summary>Determines whether the specified <see cref="MemberInfo"/> represents a property.</summary>
        public static bool IsProperty(this MemberInfo member) => member is PropertyInfo;

        /// <summary>Gets if <paramref name="member"/> is a <see cref="MethodInfo"/>, and has no parameters.</summary>
        public static bool HasNoParameters(this MemberInfo member) => HasParameters(member);

        /// <summary>Gets if <paramref name="member"/> is a <see cref="MethodInfo"/>, and has the specified parameters.</summary>
        public static bool HasParameters<T1>(this MemberInfo member) => HasParameters(member, typeof(T1));

        /// <inheritdoc cref="HasParameters{T1}"/>
        public static bool HasParameters<T1, T2>(this MemberInfo member) => HasParameters(member, typeof(T1), typeof(T2));

        /// <inheritdoc cref="HasParameters{T1}"/>
        public static bool HasParameters<T1, T2, T3>(this MemberInfo member) => HasParameters(member, typeof(T1), typeof(T2), typeof(T3));

        /// <inheritdoc cref="HasParameters{T1}"/>
        public static bool HasParameters<T1, T2, T3, T4>(this MemberInfo member) => HasParameters(member, typeof(T1), typeof(T2), typeof(T3), typeof(T4));

        static bool HasParameters(this MemberInfo member, params Type[] types)
        {

            var parameters = member switch
            {
                MethodInfo m => m.GetParameters(),
                ConstructorInfo c when !c.IsStatic => c.GetParameters(),
                _ => null
            };

            if (parameters == null || parameters.Length != types.Length)
                return false;

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != types[i])
                    return false;
            }

            return true;

        }

        /// <summary>Gets if <paramref name="member"/> returns <see cref="void"/>.</summary>
        public static bool ReturnsVoid(this MemberInfo member) =>
            Returns(member, typeof(void));

        /// <summary>Gets if <paramref name="member"/> returns <see cref="IEnumerator"/>.</summary>
        public static bool ReturnsCoroutine(this MemberInfo member) =>
            Returns<IEnumerator>(member);

        /// <summary>Gets if <paramref name="member"/> returns <typeparamref name="T"/>.</summary>
        public static bool Returns<T>(this MemberInfo member) =>
            Returns(member, typeof(T));

        /// <summary>Gets if <paramref name="member"/> returns <paramref name="type"/>.</summary>
        public static bool Returns(this MemberInfo member, Type type)
        {

            var returnType = member switch
            {
                MethodInfo m => m.ReturnType,
                PropertyInfo p => p.PropertyType,
                FieldInfo f => f.FieldType,
                _ => null
            };

            return type?.IsAssignableFrom(returnType) ?? false;

        }

        #region Friendly type name

        /// <summary>Gets the signature of this member.</summary>
        public static string GetSignature(this MemberInfo member, bool includeAccessModifiers = true)
        {
            return member switch
            {
                Type t => GetTypeSignature(t, includeAccessModifiers),
                FieldInfo f => GetFieldSignature(f, includeAccessModifiers),
                PropertyInfo p => GetPropertySignature(p, includeAccessModifiers),
                MethodInfo m => GetMethodSignature(m, includeAccessModifiers),
                EventInfo e => GetEventSignature(e, includeAccessModifiers),
                _ => member.Name
            };
        }

        private static string GetTypeSignature(Type type, bool includeAccessModifiers)
        {
            var sb = new StringBuilder();

            if (includeAccessModifiers)
                sb.Append(GetAccessModifier(type));

            if (type.IsAbstract && type.IsSealed) sb.Append("static ");
            else if (type.IsAbstract) sb.Append("abstract ");
            else if (type.IsSealed) sb.Append("sealed ");

            if (type.IsClass) sb.Append("class ");
            else if (type.IsInterface) sb.Append("interface ");
            else if (type.IsEnum) sb.Append("enum ");
            else if (type.IsValueType) sb.Append("struct ");

            sb.Append(GetFriendlyTypeName(type));
            return sb.ToString();
        }


        private static string GetFieldSignature(FieldInfo field, bool includeAccessModifiers)
        {
            var sb = new StringBuilder();

            if (includeAccessModifiers)
                sb.Append(GetAccessModifier(field));

            if (field.IsStatic) sb.Append("static ");
            if (field.IsInitOnly) sb.Append("readonly ");

            sb.Append(GetFriendlyTypeName(field.FieldType)).Append(" ").Append(field.Name);
            return sb.ToString();
        }

        private static string GetPropertySignature(PropertyInfo prop, bool includeAccessModifiers)
        {
            var sb = new StringBuilder();

            if (includeAccessModifiers)
                sb.Append(GetAccessModifier(prop.GetMethod ?? prop.SetMethod));

            if ((prop.GetMethod ?? prop.SetMethod)?.IsStatic == true)
                sb.Append("static ");

            sb.Append(GetFriendlyTypeName(prop.PropertyType)).Append(" ").Append(prop.Name);

            // only show accessor info if interesting
            var hasGetter = prop.CanRead && prop.GetMethod?.IsPublic == true;
            var hasSetter = prop.CanWrite && prop.SetMethod != null;

            if (hasSetter && (prop.SetMethod?.IsPublic ?? false) == false)
            {
                sb.Append(" { get; }");
            }
            else if (includeAccessModifiers && (hasGetter || hasSetter))
            {
                sb.Append(" { ");
                if (hasGetter) sb.Append("get; ");
                if (hasSetter) sb.Append("set; ");
                sb.Append("}");
            }

            return sb.ToString().Trim();
        }

        private static string GetMethodSignature(MethodInfo method, bool includeAccessModifiers)
        {
            var sb = new StringBuilder();

            if (includeAccessModifiers)
                sb.Append(GetAccessModifier(method));

            if (method.IsStatic) sb.Append("static ");
            if (method.IsAbstract) sb.Append("abstract ");
            if (method.IsVirtual && !method.IsFinal) sb.Append("virtual ");

            sb.Append(GetFriendlyTypeName(method.ReturnType)).Append(" ").Append(method.Name);

            if (method.IsGenericMethod)
            {
                var args = string.Join(", ", method.GetGenericArguments().Select(GetFriendlyTypeName));
                sb.Append("<").Append(args).Append(">");
            }

            var parameters = string.Join(", ",
                method.GetParameters().Select(p => GetFriendlyTypeName(p.ParameterType) + " " + p.Name));
            sb.Append("(").Append(parameters).Append(")");

            return sb.ToString();
        }

        private static string GetEventSignature(EventInfo evt, bool includeAccessModifiers)
        {
            var sb = new StringBuilder();

            if (includeAccessModifiers)
                sb.Append(GetAccessModifier(evt.AddMethod));

            if (evt.AddMethod.IsStatic) sb.Append("static ");

            sb.Append("event ").Append(GetFriendlyTypeName(evt.EventHandlerType)).Append(" ").Append(evt.Name);
            return sb.ToString();
        }

        private static string GetAccessModifier(FieldInfo field)
        {
            if (field.IsPublic) return "public ";
            if (field.IsFamily) return "protected ";
            if (field.IsAssembly) return "internal ";
            if (field.IsPrivate) return "private ";
            return "";
        }

        private static string GetAccessModifier(Type type)
        {
            if (type.IsPublic || type.IsNestedPublic) return "public ";
            if (type.IsNestedFamily) return "protected ";
            if (type.IsNotPublic || type.IsNestedAssembly) return "internal ";
            if (type.IsNestedPrivate) return "private ";
            return "";
        }

        private static string GetAccessModifier(EventInfo evt) =>
            GetAccessModifier(evt.AddMethod);

        private static string GetAccessModifier(PropertyInfo prop) =>
            GetAccessModifier(prop.GetMethod ?? prop.SetMethod);

        private static string GetAccessModifier(MethodBase method)
        {
            if (method == null) return "";
            if (method.IsPublic) return "public ";
            if (method.IsFamily) return "protected ";
            if (method.IsAssembly) return "internal ";
            if (method.IsPrivate) return "private ";
            return "";
        }

        /// <summary>Gets the friendly name of this type.</summary>
        public static string GetFriendlyTypeName(this Type type)
        {
            if (type.IsGenericParameter)
                return type.Name;

            if (type.IsGenericType)
            {
                var genericTypeName = type.Name[..type.Name.IndexOf('`')];
                var genericArgs = string.Join(", ", type.GetGenericArguments().Select(GetFriendlyTypeName));
                return $"{genericTypeName}<{genericArgs}>";
            }

            if (type.IsArray)
                return GetFriendlyTypeName(type.GetElementType()) + "[]";

            // Map CLR names to C# keywords
            return type.Name switch
            {
                "Void" => "void",
                "String" => "string",
                "Object" => "object",
                "Decimal" => "decimal",
                _ => type.IsPrimitive ? type.Name.ToLower() : type.Name
            };
        }

        #endregion

        internal static IEnumerable<FieldInfo> _GetFields(this Type type)
        {

            foreach (var field in type.GetFields(BindingFlags.GetField | BindingFlags.SetField | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                yield return field;

            if (type.BaseType != null)
                foreach (var field in _GetFields(type.BaseType))
                    yield return field;

        }

        internal static FieldInfo FindField(this Type type, string name)
        {
            var e = _GetFields(type).GetEnumerator();
            while (e.MoveNext())
                if (e.Current.Name == name)
                    return e.Current;
            return null;
        }

        internal static IEnumerable<Type> FindSubclasses<T>(bool includeAbstract = true) =>
            FindSubclasses(typeof(T), includeAbstract);

        internal static IEnumerable<Type> FindSubclasses(Type t, bool includeAbstract = true)
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (Exception)
                    {
                        return Type.EmptyTypes;
                    }
                })
                .Where(type => t.IsAssignableFrom(type) && type != t)
                .Where(type => includeAbstract || !type.IsAbstract);
        }

        internal static IEnumerable<T> FindSubclassesAndInstantiate<T>()
        {
            return FindSubclasses<T>().Where(t => t.GetConstructor(Type.EmptyTypes) is not null).Select(t => (T)Activator.CreateInstance(t));
        }

        internal static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                    return true;
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        internal static IEnumerable<MemberInfo> FindFieldsDecoratedWithAttribute<TAttribute>(this object obj, bool withProperties = false) where TAttribute : Attribute =>
            obj.GetType().FindFieldsDecoratedWithAttribute<TAttribute>(withProperties);

        internal static IEnumerable<MemberInfo> FindFieldsDecoratedWithAttribute<TAttribute>(this Type type, bool withProperties = false) where TAttribute : Attribute
        {

            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach (var field in type.GetFields(flags))
                if (field.IsDefined<TAttribute>())
                    yield return field;

            if (withProperties)
                foreach (var property in type.GetProperties(flags))
                    if (property.IsDefined<TAttribute>())
                        yield return property;

        }

        internal static bool IsDefined<TAttribute>(this MemberInfo member, bool inherit = false) =>
            member.IsDefined(typeof(TAttribute), inherit);

        internal static void SetValue(this MemberInfo member, object obj, object value)
        {
            if (member is FieldInfo field && (value is null || field.FieldType.IsAssignableFrom(value.GetType())))
            {
                field.SetValue(obj, value);
            }
            else if (member is PropertyInfo property && (value is null || property.PropertyType.IsAssignableFrom(value.GetType())))
            {
                property.SetValue(obj, value);
            }
        }
    }

}