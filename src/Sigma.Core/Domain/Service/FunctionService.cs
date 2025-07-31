using System.Reflection;
using System.Xml;
using Sigma.Core.Common;
using Sigma.Core.Utils;

namespace Sigma.Core.Domain.Service
{
    public class FunctionService
    {
        private readonly Dictionary<string, MethodInfo> _methodCache;
        private readonly Dictionary<string, (string Description, (Type ParameterType, string Description) ReturnType, (string ParameterName, Type ParameterType, string Description)[] Parameters)> _methodInfos;

        private readonly IServiceProvider _serviceProvider;
        private readonly Assembly[] _assemblies;

        public FunctionService(IServiceProvider serviceProvider, Assembly[] assemblies)
        {
            _methodCache = [];
            _methodInfos = [];
            _serviceProvider = serviceProvider;
            _assemblies = assemblies;
        }

        public Dictionary<string, MethodInfo> Functions => _methodCache;
        public Dictionary<string, (string Description, (Type ParameterType, string Description) ReturnType, (string ParameterName, Type ParameterType, string Description)[] Parameters)> MethodInfos => _methodInfos;

        /// <summary>
        /// Search delegates in assemblies. Later versions will use Source Generators.
        /// </summary>
        public void SearchMarkedMethods()
        {
            var markedMethods = new List<MethodInfo>();

            _methodCache.Clear();
            _methodInfos.Clear();

            foreach (var assembly in _assemblies)
            {
                // retrieve methods marked with ActionAttribute from cache
                foreach (var type in assembly.GetTypes())
                {
                    markedMethods.AddRange(type.GetMethods().Where(m => m.GetCustomAttributes(typeof(SigmaFunctionAttribute), true).Length > 0));
                }
            }

            // build method invocation map
            foreach (var method in markedMethods)
            {
                var key = $"{method.DeclaringType.Assembly.GetName().Name}_{method.DeclaringType.Name}_{method.Name}";
                _methodCache.TryAdd(key,  method);

                var xmlCommentHelper = new XmlCommentHelper();
                xmlCommentHelper.LoadAll();

                var description = xmlCommentHelper.GetMethodComment(method);
                var dict = xmlCommentHelper.GetParameterComments(method);

                var parameters = method.GetParameters().Select(x => (x.Name, x.ParameterType, dict[x.Name])).ToArray();
                var returnType = xmlCommentHelper.GetMethodReturnComment(method);

                _methodInfos.TryAdd(key, (description, (method.ReflectedType, returnType), parameters));
            }
        }
    }
}