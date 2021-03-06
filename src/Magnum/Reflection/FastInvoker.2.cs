// Copyright 2007-2008 The Apache Software Foundation.
//  
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use 
// this file except in compliance with the License. You may obtain a copy of the 
// License at 
// 
//     http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software distributed 
// under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR 
// CONDITIONS OF ANY KIND, either express or implied. See the License for the 
// specific language governing permissions and limitations under the License.
namespace Magnum.Reflection
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using Extensions;

	public class FastInvoker<T, TResult> :
		FastInvokerBase,
		IFastInvoker<T, TResult>
	{
		private static FastInvoker<T, TResult> _current;
		private readonly Dictionary<int, Func<T, TResult>> _noArgs = new Dictionary<int, Func<T, TResult>>();
		private readonly Dictionary<int, Func<T, object[], TResult>> _withArgs = new Dictionary<int, Func<T, object[], TResult>>();

		private FastInvoker()
			: base(typeof (T))
		{
		}

		public static FastInvoker<T, TResult> Current
		{
			get
			{
				if (_current == null)
					_current = new FastInvoker<T, TResult>();

				return _current;
			}
		}

		public TResult FastInvoke(T target, string methodName)
		{
			int key = 97 * methodName.GetHashCode();

			var invoker = GetInvoker(key, () =>
				{
					return MethodNameCache[methodName]
						.Where(x => x.ReturnType == typeof(TResult))
						.MatchingArguments()
						.First();
				});

			return invoker(target);
		}

		public TResult FastInvoke(T target, string methodName, params object[] args)
		{
			if (args == null || args.Length == 0)
				return FastInvoke(target, methodName);

			int key = GetArgumentHashCode(97 * methodName.GetHashCode(), args);

			var invoker = GetInvoker(key, () =>
				{
					return MethodNameCache[methodName]
						.Where(x => x.ReturnType == typeof (TResult))
						.MatchingArguments(args)
						.First()
						.ToSpecializedMethod(args);
				}, args);

			return invoker(target, args);
		}

		public TResult FastInvoke(T target, Type[] genericTypes, string methodName)
		{
			int key = GetArgumentHashCode(97 * methodName.GetHashCode(), genericTypes);

			var invoker = GetInvoker(key, () =>
				{
					var empty = new object[] { };

					return MethodNameCache[methodName]
						.Where(x => x.ReturnType == typeof(TResult))
						.MatchingArguments()
						.First()
						.ToSpecializedMethod(genericTypes, empty);
				});

			return invoker(target);
		}

		public TResult FastInvoke(T target, Type[] genericTypes, string methodName, object[] args)
		{
			if (args == null || args.Length == 0)
				return FastInvoke(target, genericTypes, methodName);

			int key = GetArgumentHashCode(97 * methodName.GetHashCode(), genericTypes, args);

			var invoker = GetInvoker(key, () =>
				{
					return MethodNameCache[methodName]
						.Where(x => x.ReturnType == typeof(TResult))
						.MatchingArguments(args)
						.First()
						.ToSpecializedMethod(genericTypes, args);
				}, args);

			return invoker(target, args);
		}

		public TResult FastInvoke(T target, Expression<Func<T, TResult>> expression)
		{
			var call = expression.Body as MethodCallExpression;
			if (call == null)
				throw new ArgumentException("Only method call expressions are supported.", "expression");

			int key = 61 * call.Method.GetHashCode();

			var invoker = GetInvoker(key, () => call.Method);

			return invoker(target);
		}

		public TResult FastInvoke(T target, Expression<Func<T, TResult>> expression, params object[] args)
		{
			var call = expression.Body as MethodCallExpression;
			if (call == null)
				throw new ArgumentException("Only method call expressions are supported.", "expression");

			MethodInfo method = call.Method;

			int key = GetArgumentHashCode(61 * method.GetHashCode(), args);

			var invoker = GetInvoker(key, () =>
				{
					return method.IsGenericMethod ? method.GetGenericMethodDefinition().ToSpecializedMethod(args) : method;
				}, args);

			return invoker(target, args);
		}

		public TResult FastInvoke(T target, Type[] genericTypes, Expression<Func<T, TResult>> expression)
		{
			var call = expression.Body as MethodCallExpression;
			if (call == null)
				throw new ArgumentException("Only method call expressions are supported.", "expression");

			MethodInfo method = call.Method;

			int key = GetArgumentHashCode(61 * method.GetHashCode(), genericTypes);

			var invoker = GetInvoker(key, () =>
				{
					if (method.IsGenericMethod)
						return GetGenericMethodFromTypes(method.GetGenericMethodDefinition(), genericTypes);

					return method;
				});

			return invoker(target);
		}

		public TResult FastInvoke(T target, Type[] genericTypes, Expression<Func<T, TResult>> expression, object[] args)
		{
			var call = expression.Body as MethodCallExpression;
			if (call == null)
				throw new ArgumentException("Only method call expressions are supported.", "expression");

			MethodInfo method = call.Method;

			int key = GetArgumentHashCode(61 * method.GetHashCode(), genericTypes, args);

			var invoker = GetInvoker(key, () =>
				{
					if (method.IsGenericMethod)
						return method.GetGenericMethodDefinition().ToSpecializedMethod(genericTypes, args);

					return method.ToSpecializedMethod(genericTypes, args);
				}, args);

			return invoker(target, args);
		}

		private Func<T, TResult> GetInvoker(int key, Func<MethodInfo> getMethodInfo)
		{
			return _noArgs.Retrieve(key, () =>
				{
					MethodInfo method = getMethodInfo();

					ParameterExpression instanceParameter = Expression.Parameter(typeof (T), "target");

					MethodCallExpression call = Expression.Call(instanceParameter, method);

					return Expression.Lambda<Func<T, TResult>>(call, new[] {instanceParameter}).Compile();
				});
		}

		private Func<T, object[], TResult> GetInvoker(int key, Func<MethodInfo> getMethodInfo, object[] args)
		{
			return _withArgs.Retrieve(key, () =>
				{
					MethodInfo method = getMethodInfo();

					ParameterExpression instanceParameter = Expression.Parameter(typeof (T), "target");
					ParameterExpression argsParameter = Expression.Parameter(typeof (object[]), "args");

					Expression[] parameters = method.GetParameters().ToArrayIndexParameters(argsParameter).ToArray();

					MethodCallExpression call = Expression.Call(instanceParameter, method, parameters);

					return Expression.Lambda<Func<T, object[], TResult>>(call, new[] {instanceParameter, argsParameter}).Compile();
				});
		}
	}
}