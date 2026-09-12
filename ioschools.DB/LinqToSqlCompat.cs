// LinqToSqlCompat.cs
// Compatibility shim providing System.Data.Linq types for .NET 8.
// These types replicate the LINQ to SQL API surface used by the auto-generated designer code.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.SqlClient;

// ─────────────────────────────────────────────────────────────────────────────
// System.Data.Linq.Mapping namespace
// ─────────────────────────────────────────────────────────────────────────────
namespace System.Data.Linq.Mapping
{
    public enum AutoSync
    {
        Default = 0,
        Always = 1,
        OnInsert = 2,
        Never = 3
    }

    public enum UpdateCheck
    {
        Always = 0,
        WhenChanged = 1,
        Never = 2
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class TableAttribute : Attribute
    {
        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class ColumnAttribute : Attribute
    {
        public string Storage { get; set; }
        public string Name { get; set; }
        public string DbType { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsDbGenerated { get; set; }
        public bool IsVersion { get; set; }
        public bool IsDiscriminator { get; set; }
        public bool CanBeNull { get; set; } = true;
        public AutoSync AutoSync { get; set; } = AutoSync.Default;
        public UpdateCheck UpdateCheck { get; set; } = UpdateCheck.Always;
        public bool IsNullable { get; set; } = true;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class AssociationAttribute : Attribute
    {
        public string Name { get; set; }
        public string Storage { get; set; }
        public string ThisKey { get; set; }
        public string OtherKey { get; set; }
        public bool IsForeignKey { get; set; }
        public bool IsUnique { get; set; }
        public string DeleteRule { get; set; }
        public bool DeleteOnNull { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DatabaseAttribute : Attribute
    {
        public string Name { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class FunctionAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsComposable { get; set; }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false)]
    public sealed class ParameterAttribute : Attribute
    {
        public string Name { get; set; }
        public string DbType { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ResultTypeAttribute : Attribute
    {
        public ResultTypeAttribute(Type type) { Type = type; }
        public Type Type { get; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class InheritanceMappingAttribute : Attribute
    {
        public object Code { get; set; }
        public Type Type { get; set; }
        public bool IsDefault { get; set; }
    }

    // ── MetaModel / MetaTable stubs ──────────────────────────────────────────
    public abstract class MappingSource
    {
        public abstract MetaModel GetModel(Type dataContextType);
    }

    public abstract class MetaModel
    {
        public abstract MetaTable GetTable(Type rowType);
        public abstract MappingSource MappingSource { get; }
        public abstract Type ContextType { get; }
        public abstract string DatabaseName { get; }
        public abstract IEnumerable<MetaTable> GetTables();
        public abstract MetaFunction GetFunction(MethodInfo method);
        public abstract IEnumerable<MetaFunction> GetFunctions();
        public abstract MetaType GetMetaType(Type type);
    }

    public abstract class MetaTable
    {
        public abstract string TableName { get; }
        public abstract MetaType RowType { get; }
        public abstract MetaModel Model { get; }
        public abstract MethodInfo InsertMethod { get; }
        public abstract MethodInfo UpdateMethod { get; }
        public abstract MethodInfo DeleteMethod { get; }
    }

    public abstract class MetaType
    {
        public abstract Type Type { get; }
        public abstract string Name { get; }
        public abstract bool IsEntity { get; }
        public abstract IReadOnlyList<MetaDataMember> DataMembers { get; }
        public abstract IReadOnlyList<MetaDataMember> PersistentDataMembers { get; }
        public abstract IReadOnlyList<MetaDataMember> IdentityMembers { get; }
        public abstract MetaDataMember DBGeneratedIdentityMember { get; }
        public abstract MetaDataMember VersionMember { get; }
        public abstract MetaDataMember Discriminator { get; }
        public abstract bool HasUpdateCheck { get; }
        public abstract bool IsInheritanceDefault { get; }
        public abstract MetaType InheritanceRoot { get; }
        public abstract object InheritanceCode { get; }
        public abstract IReadOnlyList<MetaType> InheritanceTypes { get; }
        public abstract IReadOnlyList<MetaType> DerivedTypes { get; }
        public abstract MetaType BaseType { get; }
        public abstract MetaTable Table { get; }
        public abstract MetaModel Model { get; }
        public abstract bool CanInstantiate { get; }
        public abstract bool HasInheritance { get; }
        public abstract bool HasAnyLoadMethod { get; }
        public abstract bool HasAnyValidateMethod { get; }
        public abstract MethodInfo OnLoadedMethod { get; }
        public abstract MethodInfo OnValidateMethod { get; }
        public abstract MetaDataMember GetDataMember(MemberInfo mi);
        public abstract MetaType GetInheritanceType(Type type);
        public abstract MetaType GetTypeForInheritanceCode(object code);
    }

    public abstract class MetaDataMember
    {
        public abstract MetaType DeclaringType { get; }
        public abstract MemberInfo Member { get; }
        public abstract MemberInfo StorageMember { get; }
        public abstract string Name { get; }
        public abstract string MappedName { get; }
        public abstract int Ordinal { get; }
        public abstract Type Type { get; }
        public abstract bool IsPersistent { get; }
        public abstract bool IsAssociation { get; }
        public abstract bool IsPrimaryKey { get; }
        public abstract bool IsDbGenerated { get; }
        public abstract bool IsVersion { get; }
        public abstract bool IsDiscriminator { get; }
        public abstract bool CanBeNull { get; }
        public abstract string DbType { get; }
        public abstract string Expression { get; }
        public abstract UpdateCheck UpdateCheck { get; }
        public abstract AutoSync AutoSync { get; }
        public abstract MetaAssociation Association { get; }
        public abstract MethodInfo LoadMethod { get; }
        public abstract bool IsDeferred { get; }
        public abstract object MemberAccessor { get; }
        public abstract object StorageAccessor { get; }
        public abstract bool IsDeclaredBy(MetaType type);
    }

    public abstract class MetaAssociation
    {
        public abstract MetaType OtherType { get; }
        public abstract MetaDataMember ThisMember { get; }
        public abstract MetaDataMember OtherMember { get; }
        public abstract IReadOnlyList<MetaDataMember> ThisKey { get; }
        public abstract IReadOnlyList<MetaDataMember> OtherKey { get; }
        public abstract bool IsMany { get; }
        public abstract bool IsForeignKey { get; }
        public abstract bool IsUnique { get; }
        public abstract bool IsNullable { get; }
        public abstract bool ThisKeyIsPrimaryKey { get; }
        public abstract bool OtherKeyIsPrimaryKey { get; }
        public abstract string DeleteRule { get; }
        public abstract bool DeleteOnNull { get; }
    }

    public abstract class MetaFunction
    {
        public abstract MetaModel Model { get; }
        public abstract MethodInfo Method { get; }
        public abstract string MappedName { get; }
        public abstract string Name { get; }
        public abstract bool IsComposable { get; }
        public abstract IReadOnlyList<MetaParameter> Parameters { get; }
        public abstract MetaParameter ReturnParameter { get; }
        public abstract bool HasMultipleResults { get; }
        public abstract IEnumerable<MetaType> ResultRowTypes { get; }
    }

    public abstract class MetaParameter
    {
        public abstract MetaFunction Method { get; }
        public abstract ParameterInfo Parameter { get; }
        public abstract string Name { get; }
        public abstract string MappedName { get; }
        public abstract Type ParameterType { get; }
        public abstract string DbType { get; }
    }

    // ── AttributeMappingSource ───────────────────────────────────────────────
    public sealed class AttributeMappingSource : MappingSource
    {
        private readonly Dictionary<Type, MetaModel> _cache = new Dictionary<Type, MetaModel>();

        public override MetaModel GetModel(Type dataContextType)
        {
            if (!_cache.TryGetValue(dataContextType, out var model))
            {
                model = new AttributeMetaModel(this, dataContextType);
                _cache[dataContextType] = model;
            }
            return model;
        }
    }

    // Minimal concrete MetaModel used by AttributeMappingSource
    internal sealed class AttributeMetaModel : MetaModel
    {
        private readonly MappingSource _source;
        private readonly Type _contextType;

        public AttributeMetaModel(MappingSource source, Type contextType)
        {
            _source = source;
            _contextType = contextType;
        }

        public override MappingSource MappingSource => _source;
        public override Type ContextType => _contextType;
        public override string DatabaseName => _contextType.Name;

        public override MetaTable GetTable(Type rowType)
            => new AttributeMetaTable(rowType, this);

        public override IEnumerable<MetaTable> GetTables()
            => Enumerable.Empty<MetaTable>();

        public override MetaFunction GetFunction(MethodInfo method) => null;
        public override IEnumerable<MetaFunction> GetFunctions() => Enumerable.Empty<MetaFunction>();
        public override MetaType GetMetaType(Type type) => null;
    }

    internal sealed class AttributeMetaTable : MetaTable
    {
        private readonly Type _rowType;
        private readonly MetaModel _model;

        public AttributeMetaTable(Type rowType, MetaModel model)
        {
            _rowType = rowType;
            _model = model;
            var attr = rowType.GetCustomAttribute<TableAttribute>();
            TableName = attr?.Name ?? rowType.Name;
        }

        public override string TableName { get; }
        public override MetaType RowType => null;
        public override MetaModel Model => _model;
        public override MethodInfo InsertMethod => null;
        public override MethodInfo UpdateMethod => null;
        public override MethodInfo DeleteMethod => null;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// System.Data.Linq namespace
// ─────────────────────────────────────────────────────────────────────────────
namespace System.Data.Linq
{
    using System.Data.Linq.Mapping;

    // ── Enums ────────────────────────────────────────────────────────────────
    public enum ChangeAction
    {
        None = 0,
        Delete = 1,
        Insert = 2,
        Update = 3
    }

    // ── Exceptions ───────────────────────────────────────────────────────────
    public class ChangeConflictException : Exception
    {
        public ChangeConflictException() { }
        public ChangeConflictException(string message) : base(message) { }
        public ChangeConflictException(string message, Exception inner) : base(message, inner) { }
    }

    public class ForeignKeyReferenceAlreadyHasValueException : InvalidOperationException
    {
        public ForeignKeyReferenceAlreadyHasValueException()
            : base("Foreign key reference already has a value.") { }
    }

    // ── ModifiedMemberInfo ───────────────────────────────────────────────────
    public sealed class ModifiedMemberInfo
    {
        public ModifiedMemberInfo(MemberInfo member, object current, object original)
        {
            Member = member;
            CurrentValue = current;
            OriginalValue = original;
        }
        public MemberInfo Member { get; }
        public object CurrentValue { get; }
        public object OriginalValue { get; }
    }

    // ── MemberChangeConflict ─────────────────────────────────────────────────
    public sealed class MemberChangeConflict
    {
        public MemberInfo Member { get; internal set; }
        public object CurrentValue { get; internal set; }
        public object OriginalValue { get; internal set; }
        public object DatabaseValue { get; internal set; }
        public bool IsModified { get; internal set; }
        public bool IsResolved { get; internal set; }
        public void Resolve(RefreshMode refreshMode) { }
        public void Resolve(object value) { }
    }

    // ── ObjectChangeConflict ─────────────────────────────────────────────────
    public sealed class ObjectChangeConflict
    {
        public object Object { get; internal set; }
        public bool IsDeleted { get; internal set; }
        public bool IsResolved { get; internal set; }
        public IList<MemberChangeConflict> MemberConflicts { get; internal set; }
            = new List<MemberChangeConflict>();
        public void Resolve() { }
        public void Resolve(RefreshMode refreshMode) { }
        public void Resolve(RefreshMode refreshMode, object changedObject) { }
    }

    // ── ChangeConflictCollection ─────────────────────────────────────────────
    public sealed class ChangeConflictCollection : ICollection<ObjectChangeConflict>
    {
        private readonly List<ObjectChangeConflict> _list = new List<ObjectChangeConflict>();
        public int Count => _list.Count;
        public bool IsReadOnly => false;
        public void Add(ObjectChangeConflict item) => _list.Add(item);
        public void Clear() => _list.Clear();
        public bool Contains(ObjectChangeConflict item) => _list.Contains(item);
        public void CopyTo(ObjectChangeConflict[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public bool Remove(ObjectChangeConflict item) => _list.Remove(item);
        public IEnumerator<ObjectChangeConflict> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        public void ResolveAll(RefreshMode mode) { }
        public void ResolveAll(RefreshMode mode, bool autoResolveDeletes) { }
    }

    // ── RefreshMode ──────────────────────────────────────────────────────────
    public enum RefreshMode
    {
        KeepCurrentValues = 0,
        KeepChanges = 1,
        OverwriteCurrentValues = 2
    }

    // ── ConflictMode ─────────────────────────────────────────────────────────
    public enum ConflictMode
    {
        FailOnFirstConflict = 0,
        ContinueOnConflict = 1
    }

    // ── DataLoadOptions ──────────────────────────────────────────────────────
    public sealed class DataLoadOptions
    {
        public void LoadWith<T>(Expression<Func<T, object>> expression) { }
        public void LoadWith(LambdaExpression expression) { }
        public void AssociateWith<T>(Expression<Func<T, object>> expression) { }
        public void AssociateWith(LambdaExpression expression) { }
    }

    // ── ITable ───────────────────────────────────────────────────────────────
    public interface ITable : IQueryable
    {
        DataContext Context { get; }
        bool IsReadOnly { get; }
        void InsertOnSubmit(object entity);
        void InsertAllOnSubmit(IEnumerable entities);
        void Attach(object entity);
        void Attach(object entity, bool asModified);
        void Attach(object entity, object original);
        void AttachAll(IEnumerable entities);
        void AttachAll(IEnumerable entities, bool asModified);
        void DeleteOnSubmit(object entity);
        void DeleteAllOnSubmit(IEnumerable entities);
        object GetOriginalEntityState(object entity);
        IEnumerable<ModifiedMemberInfo> GetModifiedMembers(object entity);
    }

    // ── Table<T> ─────────────────────────────────────────────────────────────
    public sealed class Table<TEntity> : ITable, IQueryable<TEntity>, IEnumerable<TEntity>
        where TEntity : class
    {
        private readonly DataContext _context;
        private readonly List<TEntity> _insertList = new List<TEntity>();
        private readonly List<TEntity> _deleteList = new List<TEntity>();

        internal Table(DataContext context) { _context = context; }

        public DataContext Context => _context;
        public bool IsReadOnly => false;

        // IQueryable
        public Type ElementType => typeof(TEntity);
        public Expression Expression => Expression.Constant(this);
        public IQueryProvider Provider => Enumerable.Empty<TEntity>().AsQueryable().Provider;

        public IEnumerator<TEntity> GetEnumerator() => Enumerable.Empty<TEntity>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void InsertOnSubmit(TEntity entity) => _insertList.Add(entity);
        public void InsertAllOnSubmit<TSubEntity>(IEnumerable<TSubEntity> entities)
            where TSubEntity : TEntity
        {
            foreach (var e in entities) _insertList.Add(e);
        }

        public void DeleteOnSubmit(TEntity entity) => _deleteList.Add(entity);
        public void DeleteAllOnSubmit<TSubEntity>(IEnumerable<TSubEntity> entities)
            where TSubEntity : TEntity
        {
            foreach (var e in entities) _deleteList.Add(e);
        }

        public void Attach(TEntity entity) { }
        public void Attach(TEntity entity, bool asModified) { }
        public void Attach(TEntity entity, TEntity original) { }
        public void AttachAll<TSubEntity>(IEnumerable<TSubEntity> entities) where TSubEntity : TEntity { }
        public void AttachAll<TSubEntity>(IEnumerable<TSubEntity> entities, bool asModified) where TSubEntity : TEntity { }

        public TEntity GetOriginalEntityState(TEntity entity) => null;
        public IEnumerable<ModifiedMemberInfo> GetModifiedMembers(TEntity entity)
            => Enumerable.Empty<ModifiedMemberInfo>();

        // ITable explicit
        void ITable.InsertOnSubmit(object entity) => InsertOnSubmit((TEntity)entity);
        void ITable.InsertAllOnSubmit(IEnumerable entities)
        {
            foreach (TEntity e in entities) _insertList.Add(e);
        }
        void ITable.Attach(object entity) => Attach((TEntity)entity);
        void ITable.Attach(object entity, bool asModified) => Attach((TEntity)entity, asModified);
        void ITable.Attach(object entity, object original) => Attach((TEntity)entity, (TEntity)original);
        void ITable.AttachAll(IEnumerable entities) { }
        void ITable.AttachAll(IEnumerable entities, bool asModified) { }
        void ITable.DeleteOnSubmit(object entity) => DeleteOnSubmit((TEntity)entity);
        void ITable.DeleteAllOnSubmit(IEnumerable entities)
        {
            foreach (TEntity e in entities) _deleteList.Add(e);
        }
        object ITable.GetOriginalEntityState(object entity) => GetOriginalEntityState((TEntity)entity);
        IEnumerable<ModifiedMemberInfo> ITable.GetModifiedMembers(object entity)
            => GetModifiedMembers((TEntity)entity);
    }

    // ── EntitySet<T> ─────────────────────────────────────────────────────────
    public sealed class EntitySet<TEntity> : IList<TEntity>, ICollection<TEntity>, IEnumerable<TEntity>
        where TEntity : class
    {
        private readonly List<TEntity> _list = new List<TEntity>();
        private readonly Action<TEntity> _onAdd;
        private readonly Action<TEntity> _onRemove;
        private bool _hasLoadedOrAssignedValue;

        public EntitySet() { }

        public EntitySet(Action<TEntity> onAdd, Action<TEntity> onRemove)
        {
            _onAdd = onAdd;
            _onRemove = onRemove;
        }

        public bool HasLoadedOrAssignedValue => _hasLoadedOrAssignedValue;

        public void Assign(EntitySet<TEntity> entitySet)
        {
            _list.Clear();
            if (entitySet != null)
            {
                foreach (var item in entitySet)
                    Add(item);
            }
            _hasLoadedOrAssignedValue = true;
        }

        public void Assign(IEnumerable<TEntity> entities)
        {
            _list.Clear();
            if (entities != null)
            {
                foreach (var item in entities)
                    Add(item);
            }
            _hasLoadedOrAssignedValue = true;
        }

        // IList<T>
        public TEntity this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public void Add(TEntity entity)
        {
            if (!_list.Contains(entity))
            {
                _list.Add(entity);
                _onAdd?.Invoke(entity);
                _hasLoadedOrAssignedValue = true;
            }
        }

        public void Remove(TEntity entity)
        {
            if (_list.Remove(entity))
                _onRemove?.Invoke(entity);
        }

        bool ICollection<TEntity>.Remove(TEntity entity)
        {
            bool removed = _list.Remove(entity);
            if (removed) _onRemove?.Invoke(entity);
            return removed;
        }

        public void Clear()
        {
            var copy = _list.ToList();
            _list.Clear();
            foreach (var e in copy) _onRemove?.Invoke(e);
        }

        public bool Contains(TEntity entity) => _list.Contains(entity);
        public void CopyTo(TEntity[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public int IndexOf(TEntity entity) => _list.IndexOf(entity);
        public void Insert(int index, TEntity entity) => _list.Insert(index, entity);
        public void RemoveAt(int index)
        {
            var entity = _list[index];
            _list.RemoveAt(index);
            _onRemove?.Invoke(entity);
        }

        public IEnumerator<TEntity> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
    }

    // ── EntityRef<T> ─────────────────────────────────────────────────────────
    public struct EntityRef<TEntity> where TEntity : class
    {
        private TEntity _entity;
        private bool _hasLoadedOrAssignedValue;

        public TEntity Entity
        {
            get => _entity;
            set
            {
                _entity = value;
                _hasLoadedOrAssignedValue = true;
            }
        }

        public bool HasLoadedOrAssignedValue => _hasLoadedOrAssignedValue;

        public bool HasValue => _entity != null;
    }

    // ── DataContext ───────────────────────────────────────────────────────────
    public class DataContext : IDisposable
    {
        private readonly Dictionary<Type, object> _tables = new Dictionary<Type, object>();
        private bool _disposed;

        public DataContext(string connectionString)
        {
            Connection = new SqlConnection(connectionString);
            Mapping = new AttributeMappingSource().GetModel(GetType());
        }

        public DataContext(string connectionString, MappingSource mappingSource)
        {
            Connection = new SqlConnection(connectionString);
            Mapping = mappingSource.GetModel(GetType());
        }

        public DataContext(IDbConnection connection)
        {
            Connection = connection;
            Mapping = new AttributeMappingSource().GetModel(GetType());
        }

        public DataContext(IDbConnection connection, MappingSource mappingSource)
        {
            Connection = connection;
            Mapping = mappingSource.GetModel(GetType());
        }

        public IDbConnection Connection { get; }
        public MetaModel Mapping { get; private set; }
        public ChangeConflictCollection ChangeConflicts { get; } = new ChangeConflictCollection();
        public DataLoadOptions LoadOptions { get; set; }
        public bool DeferredLoadingEnabled { get; set; } = true;
        public bool ObjectTrackingEnabled { get; set; } = true;
        public int CommandTimeout { get; set; } = 30;
        public System.IO.TextWriter Log { get; set; }
        public IDbTransaction Transaction { get; set; }

        public Table<TEntity> GetTable<TEntity>() where TEntity : class
        {
            var type = typeof(TEntity);
            if (!_tables.TryGetValue(type, out var table))
            {
                table = new Table<TEntity>(this);
                _tables[type] = table;
            }
            return (Table<TEntity>)table;
        }

        public ITable GetTable(Type entityType)
        {
            if (!_tables.TryGetValue(entityType, out var table))
            {
                var tableType = typeof(Table<>).MakeGenericType(entityType);
                table = Activator.CreateInstance(tableType, this);
                _tables[entityType] = table;
            }
            return (ITable)table;
        }

        public virtual void SubmitChanges() { SubmitChanges(ConflictMode.FailOnFirstConflict); }
        public virtual void SubmitChanges(ConflictMode failureMode) { }

        public DbCommand GetCommand(IQueryable query)
        {
            if (Connection is DbConnection dbConn)
                return dbConn.CreateCommand();
            return new SqlCommand();
        }

        public void Refresh(RefreshMode mode, object entity) { }
        public void Refresh(RefreshMode mode, IEnumerable entities) { }
        public void Refresh(RefreshMode mode, params object[] entities) { }

        public IEnumerable<TResult> ExecuteQuery<TResult>(string query, params object[] parameters)
            => Enumerable.Empty<TResult>();

        public int ExecuteCommand(string command, params object[] parameters) => 0;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                Connection?.Dispose();
                _disposed = true;
            }
        }
    }
}
