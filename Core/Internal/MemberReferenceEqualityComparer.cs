using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Cilin.Core.Internal {
    public class MemberReferenceEqualityComparer : IEqualityComparer<MemberReference> {
        public static MemberReferenceEqualityComparer Default { get; } = new MemberReferenceEqualityComparer();

        public bool Equals(MemberReference x, MemberReference y) {
            if (x == y)
                return true;

            var result = EqualsAs<TypeReference>(x, y, Equals)
                      ?? EqualsAs<MethodReference>(x, y, Equals)
                      ?? EqualsAs<FieldReference>(x, y, Equals);

            if (result == null)
                throw new NotImplementedException("Equals, " + x.GetType() + ", " + y.GetType());

            return result.Value;
        }

        private bool? EqualsAs<T>(MemberReference x, MemberReference y, Func<T, T, bool> equals)
            where T : class
        {
            var xAsT = x as T;
            if (xAsT != null) {
                var yAsT = y as T;
                if (yAsT == null)
                    return false;

                return equals(xAsT, yAsT);
            }

            return null;
        }

        private bool Equals(TypeReference x, TypeReference y) {
            if (x.IsGenericInstance || y.IsGenericInstance || x.IsGenericParameter || y.IsGenericParameter)
                return false; // this is cached separately as parameter resolutions might depend on the scope

            if (x.Namespace != y.Namespace)
                return false;

            if (x.Name != y.Name)
                return false;

            /*if (!(EqualsAs<GenericParameter>(x, y, (xp, yp) => xp == yp) ?? true))
                return false;

            if (!(EqualsAs<IGenericInstance>(x, y, EqualsByGenericParameters) ?? true))
                return false;*/

            return true;
        }

        private bool Equals(MethodReference x, MethodReference y) {
            if (!Equals(x.DeclaringType, y.DeclaringType))
                return false;

            if (x.Name != y.Name)
                return false;

            if (x.HasParameters != y.HasParameters)
                return false;

            if (!x.Parameters.Select(p => p.ParameterType).SequenceEqual(y.Parameters.Select(p => p.ParameterType), this))
                return false;

            if (!(EqualsAs<IGenericInstance>(x, y, EqualsByGenericParameters) ?? true))
                return false;

            return true;
        }

        private bool EqualsByGenericParameters(IGenericInstance x, IGenericInstance y) {
            return x.GenericArguments.SequenceEqual(y.GenericArguments, this);
        }

        private bool Equals(FieldReference x, FieldReference y) {
            if (!Equals(x.DeclaringType, y.DeclaringType))
                return false;

            if (x.Name != y.Name)
                return false;

            return true;
        }

        public int GetHashCode(MemberReference obj) {
            var type = obj as TypeReference;
            if (type != null) {
                if (type.IsGenericInstance || type.IsGenericParameter)
                    return type.GetHashCode();

                var definition = obj as TypeDefinition;
                if (definition != null)
                    return definition.Module.FullyQualifiedName.GetHashCode() ^ definition.MetadataToken.ToInt32();

                var hashCode = type.Namespace.GetHashCode() ^ type.Name.GetHashCode();
                /*var parameter = type as GenericParameter;
                if (parameter != null)
                    hashCode ^= parameter.GetHashCode();

                var generic = type as GenericInstanceType;
                if (generic != null) {
                    foreach (var argument in generic.GenericArguments) {
                        hashCode ^= GetHashCode(argument);
                    }
                }*/

                return hashCode;
            }

            var method = obj as MethodReference;
            if (method != null) {
                var definition = obj as MethodDefinition;
                if (definition != null)
                    return definition.Module.FullyQualifiedName.GetHashCode() ^ definition.MetadataToken.ToInt32();

                var hashCode = GetHashCode(method.DeclaringType)
                             ^ method.Name.GetHashCode()
                             ^ method.ReturnType.GetHashCode();

                if (method.HasParameters) {
                    foreach (var parameter in method.Parameters) {
                        if ((parameter.ParameterType as GenericParameter)?.Owner == method) {
                            hashCode ^= parameter.Index;
                            continue;
                        }

                        hashCode ^= GetHashCode(parameter.ParameterType);
                    }
                }

                var generic = method as GenericInstanceMethod;
                if (generic != null) {
                    foreach (var parameter in generic.GenericArguments) {
                        hashCode ^= GetHashCode(parameter);
                    }
                }

                return hashCode;
            }

            var field = obj as FieldReference;
            if (field != null)
                return GetHashCode(field.DeclaringType) ^ field.Name.GetHashCode();

            throw new NotImplementedException("GetHashCode, " + obj.GetType());
        }
    }
}
