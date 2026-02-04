using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace Viper.test
{
    public static class AsyncQueryable
    {
        /// <summary>
        /// Returns the input typed as IQueryable that can be queried asynchronously
        /// </summary>
        /// <typeparam name="TEntity">The item type</typeparam>
        /// <param name="source">The input</param>
        public static IQueryable<TEntity> AsAsyncQueryable<TEntity>(this IEnumerable<TEntity> source)
            => new AsyncQueryable<TEntity>(source ?? throw new ArgumentNullException(nameof(source)));
    }

    public class AsyncQueryable<TEntity> : EnumerableQuery<TEntity>, IAsyncEnumerable<TEntity>, IQueryable<TEntity>
    {
        public AsyncQueryable(IEnumerable<TEntity> enumerable) : base(enumerable) { }
        public AsyncQueryable(Expression expression) : base(expression) { }
        public IAsyncEnumerator<TEntity> GetEnumerator() => new AsyncEnumerator(this.AsEnumerable().GetEnumerator());
        public IAsyncEnumerator<TEntity> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new AsyncEnumerator(this.AsEnumerable().GetEnumerator());
        IQueryProvider IQueryable.Provider => new AsyncQueryProvider(this);

        class AsyncEnumerator : IAsyncEnumerator<TEntity>
        {
            private readonly IEnumerator<TEntity> inner;
            public AsyncEnumerator(IEnumerator<TEntity> inner) => this.inner = inner;
            public TEntity Current => inner.Current;
            public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(inner.MoveNext());
#pragma warning disable CS1998 // Nothing to await
            public async ValueTask DisposeAsync() => inner.Dispose();
#pragma warning restore CS1998
        }

        class AsyncQueryProvider : IAsyncQueryProvider
        {
            private readonly IQueryProvider inner;
            internal AsyncQueryProvider(IQueryProvider inner) => this.inner = inner;
            public IQueryable CreateQuery(Expression expression) => new AsyncQueryable<TEntity>(expression);
            public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new AsyncQueryable<TElement>(expression);
#pragma warning disable CS8603 // Possible null reference return.
            public object Execute(Expression expression) => inner.Execute(expression);
#pragma warning restore CS8603 // Possible null reference return.
            public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);
            TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
            {
                var resultType = typeof(TResult);

                // EF Core wraps async results in Task<T>, so we need to unwrap and re-wrap
                if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var innerType = resultType.GetGenericArguments()[0];
                    var executeMethod = typeof(IQueryProvider).GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)]);
                    var genericExecute = executeMethod!.MakeGenericMethod(innerType);
                    var result = genericExecute.Invoke(inner, [expression]);

                    // Create Task.FromResult<T>(result)
                    var fromResultMethod = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(innerType);
                    return (TResult)fromResultMethod.Invoke(null, [result])!;
                }

                return Execute<TResult>(expression);
            }
        }
    }
}
