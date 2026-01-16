using Postica.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using static UnityEditor.Progress;

namespace Postica.BindingSystem.Reflection
{

    public enum Hide
    {
        Completely,
        InternalsOnly,
        ShowOnlyOnce,
    }

    public static class ReflectionFactory
    {
        public const int MAX_DEPTH = 3;
        public const int MAX_TOTAL_DEPTH = 14;
        public const int MAX_SAME_TYPE_AND_NAME_DEPTH = 1;
        public const char SEPARATOR = '/';

        public class Options
        {
            public bool ShowPrivateMembers = false;
            public bool AllowDeprecatedMembers = false;
            public bool UseNiceNames = true;
            public bool IncludeMethods = false;
            public int MaxDepth = 2;
            public int MaxTotalDepth = 5;
        }

        public class OptionsOverrides
        {
            public bool? ShowPrivateMembers;
            public bool? AllowDeprecatedMembers;
            public bool? UseNiceNames;
            public bool? IncludeMethods;
            public int? MaxDepth;
            public int? MaxTotalDepth;
        }

        public class Override
        {
            public bool? isVisible;
            public bool? canRead;
            public bool? canWrite;
            public bool? isTypeOpaque;
            public bool? isMemberOpaque;
            public bool? isMemberUnique;
        }

        private static readonly Options _options = new();
        private static bool _totalMaxDepthSet;

        private static readonly Dictionary<Type, ReflectionGroup> _itemsCache = new();
        private static readonly Dictionary<Type, List<MemberInfo>> _membersCache = new();
        
        private static readonly Dictionary<MemberInfo, Override> _overrides = new();
        
        private static readonly HashSet<Type> _hiddenTypes = new() { typeof(Type), typeof(Task) };
        private static readonly HashSet<Type> _opaqueTypes = new() { typeof(Type), typeof(Task) };
        private static readonly MembersFilter _hiddenMembers = new();
        private static readonly MembersFilter _opaqueMembers = new();
        private static readonly MembersFilter _uniqueMembers = new();
        private static readonly List<IFilter> _filters = new()
        {
            new Filter<MethodInfo>(m => !m.Attributes.HasFlag(MethodAttributes.SpecialName)),
            new Filter<MemberInfo>(m =>
            {
                var attribute = m.GetCustomAttribute<EditorBrowsableAttribute>();
                return attribute == null || attribute.State == EditorBrowsableState.Always;
            }),
            //new Filter<PropertyInfo>(p => !p.IsSpecialName),
            //new Filter<PropertyInfo>(p => p.GetIndexParameters().Length == 0), //<-- We do not allow indexers for now
            //new Filter<PropertyInfo>(p => !p.PropertyType.IsGenericType), //<-- We do not allow generics for now
            // new Filter<FieldInfo>(f => !f.FieldType.IsGenericType), //<-- We do not allow generics for now
        };

        public static Options CurrentOptions => _options;
        
        public static event Action<Options> OnOptionsChanged;
        public static event Action OnClearCache;

        public static void ChangeOptions(OptionsOverrides options)
        {
            _options.IncludeMethods = options.IncludeMethods ?? _options.IncludeMethods;
            _options.ShowPrivateMembers = options.ShowPrivateMembers ?? _options.ShowPrivateMembers;
            _options.UseNiceNames = options.UseNiceNames ?? _options.UseNiceNames;
            _options.AllowDeprecatedMembers = options.AllowDeprecatedMembers ?? _options.AllowDeprecatedMembers;
            _options.MaxDepth = Mathf.Clamp(options.MaxDepth ?? _options.MaxDepth, 0, MAX_DEPTH);
            _options.MaxTotalDepth = Mathf.Clamp(options.MaxTotalDepth ?? _options.MaxTotalDepth, 0, MAX_TOTAL_DEPTH);
            
            _totalMaxDepthSet |= options.MaxTotalDepth.HasValue;
            
            OnOptionsChanged?.Invoke(_options);
        }

        public static void HideTypes(IEnumerable<Type> types, Hide whatToHide = Hide.Completely)
        {
            switch (whatToHide)
            {
                case Hide.Completely:
                    foreach (Type type in types)
                    {
                        _hiddenTypes.Add(type);
                    }
                    break;
                case Hide.InternalsOnly:
                    foreach (Type type in types)
                    {
                        _opaqueTypes.Add(type);
                    }
                    break;
            }
        }

        public static void HideTypeTreeOfRoot<T>(Hide whatToHide = Hide.Completely) => HideTypeTreeOfRoot(typeof(T), whatToHide);

        public static void HideTypeTreeOfRoot(Type rootType, Hide whatToHide = Hide.Completely)
        {
            switch (whatToHide)
            {
                case Hide.Completely:
                    _hiddenTypes.Add(rootType);
                    foreach (var derivedType in UnityEditor.TypeCache.GetTypesDerivedFrom(rootType))
                    {
                        _hiddenTypes.Add(derivedType);
                    }
                    break;
                case Hide.InternalsOnly:
                    _opaqueTypes.Add(rootType);
                    foreach (var derivedType in UnityEditor.TypeCache.GetTypesDerivedFrom(rootType))
                    {
                        _opaqueTypes.Add(derivedType);
                    }
                    break;
            }
        }

        public static void HideMember<T>(string memberName, Hide whatToHide = Hide.Completely, bool includeDerivedClasses = false) 
            => HideMember(typeof(T), memberName, whatToHide, includeDerivedClasses);

        public static void HideMember(Type type, string memberName, Hide whatToHide = Hide.Completely, bool includeDerivedClasses = false)
        {
            MemberInfo member = null;
            try
            {
                member = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                 .FirstOrDefault(m => m.Name == memberName);

                switch (whatToHide)
                {
                    case Hide.Completely: _hiddenMembers.Add(type, member, includeDerivedClasses); break;
                    case Hide.InternalsOnly: _opaqueMembers.Add(type, member, includeDerivedClasses); break;
                    case Hide.ShowOnlyOnce: _uniqueMembers.Add(type, member, includeDerivedClasses); break;
                }
            }
            catch (Exception) when (member == null)
            {
                Debug.LogError($"{nameof(ReflectionFactory)}: Cannot find member '{memberName}' on type '{type.FullName}'");
            }
        }

        public static void HideMemberOf<TType>(Expression<Func<TType, object>> expression, Hide whatToHide = Hide.Completely, bool includeDerivedClasses = false)
        {
            var member = GetMemberInfo(expression);

            switch (whatToHide)
            {
                case Hide.Completely: _hiddenMembers.Add(typeof(TType), member, includeDerivedClasses); break;
                case Hide.InternalsOnly:_opaqueMembers.Add(typeof(TType), member, includeDerivedClasses); break;
                case Hide.ShowOnlyOnce: _uniqueMembers.Add(typeof(TType), member, includeDerivedClasses); break;
            }
        }
        
        public static void OverrideMemberOf<TType>(Expression<Func<TType, object>> expression, Override @override)
        {
            var member = GetMemberInfo(expression);

            _overrides[member] = @override;
        }

        private static MemberInfo GetMemberInfo<TType>(Expression<Func<TType, object>> expression)
        {
            MemberExpression memberExpr = expression.Body as MemberExpression;

            if (expression.Body is UnaryExpression unaryBody)
            {
                if (unaryBody.NodeType != ExpressionType.Convert &&
                    unaryBody.NodeType != ExpressionType.ConvertChecked)
                {
                    throw new ArgumentException("A Non-Convert Unary Expression was found.");
                }

                memberExpr = unaryBody.Operand as MemberExpression;
                if (memberExpr == null)
                {
                    throw new ArgumentException
                        ("The target of the Convert operation was not a MemberExpression.");
                }
            }
            else if (memberExpr == null)
            {
                throw new ArgumentException("The Expression must identify a single member.");
            }

            var member = memberExpr.Member;
            if (!(member is FieldInfo || member is PropertyInfo || member is MethodInfo))
            {
                throw new ArgumentException
                    ("The member specified was not a Field, Property or Method: " + member.GetType());
            }

            return member;
        }

        public static void AddFilter<T>(Predicate<T> filter) where T : MemberInfo
        {
            _filters.Add(new Filter<T>(filter));
        }

        public static void AddFilter(IFilter filter) => _filters.Add(filter);

        public static IReadOnlyList<MemberInfo> GetMembers(Type type)
        {
            if(!_membersCache.TryGetValue(type, out var members))
            {
                var flags = BindingFlags.Instance | BindingFlags.Public;
                if (_options.ShowPrivateMembers)
                {
                    flags |= BindingFlags.NonPublic;
                }
                
                IEnumerable<MemberInfo> query = type.GetMembers(flags);

                if (!_options.AllowDeprecatedMembers)
                {
                    query = query.Where(m => m.GetCustomAttribute<ObsoleteAttribute>() == null);
                }

                if (!_options.IncludeMethods)
                {
                    query = query.Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field);
                }

                members = query.ToList();
                _membersCache[type] = members;
            }
            return members;
        }

        public interface IFilter
        {
            bool IsValid(MemberInfo member);
        }

        private class Filter<T> : IFilter where T : MemberInfo
        {
            private readonly Predicate<T> _filter;

            public Filter(Predicate<T> filter)
            {
                _filter = filter;
            }

            bool IFilter.IsValid(MemberInfo member) => !(member is T tmember) || _filter(tmember);
        }

        internal static bool IsValid(MemberInfo member)
        {
            foreach(var filter in _filters)
            {
                if (!filter.IsValid(member))
                {
                    return false;
                }
            }
            return true;
        }
        
        internal static bool IsOpaqueMember(Type type, MemberInfo member)
        {
            return _opaqueMembers.Contains(type, member) 
                   || member.GetCustomAttribute<HideMemberAttribute>()?.HowToHide == HideMemberAttribute.Hide.InternalsOnly;
        }

        internal static bool IsOpaqueType(Type type)
        {
            return _opaqueTypes.Contains(type)
                   || type.GetCustomAttribute<HideMemberAttribute>()?.HowToHide == HideMemberAttribute.Hide.InternalsOnly;
        }

        internal static bool IsHiddenMember(Type type, MemberInfo member)
        {
            return _hiddenMembers.Contains(type, member)
                   || member.GetCustomAttribute<HideMemberAttribute>()?.HowToHide == HideMemberAttribute.Hide.Completely;
        }

        internal static bool IsHiddenType(Type type)
        {
            return _hiddenTypes.Contains(type)
                   || type.GetCustomAttribute<HideMemberAttribute>()?.HowToHide == HideMemberAttribute.Hide.Completely;
        }
        
        public static void ClearCache()
        {
            _itemsCache.Clear();
            _membersCache.Clear();
            _totalMaxDepthSet = false;
            
            OnClearCache?.Invoke();
        }

        private static ReflectionGroup GetReflectionGroup(Type type)
        {
            if (!_totalMaxDepthSet)
            {
                _options.MaxTotalDepth = BindingSettings.Current.MaxBindPathDepth;
                _totalMaxDepthSet = true;
            }
            if(!_itemsCache.TryGetValue(type, out var group))
            {
                group = new ReflectionGroup(type);
                _itemsCache[type] = group;
            }
            return group;
        }
        
        public static ReflectionGroup GetGroupFor(Type type)
        {
            if (!_totalMaxDepthSet)
            {
                _options.MaxTotalDepth = BindingSettings.Current.MaxBindPathDepth;
                _totalMaxDepthSet = true;
            }
            if (!_itemsCache.TryGetValue(type, out var group))
            {
                group = new ReflectionGroup(type);
                _itemsCache[type] = group;
            }
            group.Initialize();
            return group;
        }

        public class ReflectionGroup
        {
            public readonly Type type;
            public readonly MemberItem root;

            private bool _ready;
            private List<MemberItem> _allMembers;
            private List<MemberItem> _members;

            public bool IsInitialized => _ready;

            public IReadOnlyList<MemberItem> Members => _members;

            public List<MemberItem> AllMembers
            {
                get
                {
                    if (_allMembers == null)
                    {
                        _allMembers = new List<MemberItem>();
                        root.FlatHierarchy(AllMembers, CurrentOptions.MaxDepth);
                    }
                    return _allMembers;
                }
            }

            private ReflectionGroup()
            {
                // Hide it
            }

            internal ReflectionGroup(Type type)
            {
                this.type = type;
                root = new MemberItem("", "", 0, type, null, null);
            }

            internal void Initialize()
            {
                if (IsInitialized)
                {
                    return;
                }

                _ready = true;
                AttachRecursivelyTo(root, 0);
            }

            private void AttachRecursivelyTo(MemberItem externalRoot, int currentDepth)
            {
                if (currentDepth > CurrentOptions.MaxTotalDepth)
                {
                    return;
                }
                
                BuildSelfMembersIfNeeded();

                bool ShouldRecurse(MemberItem item)
                {
                    if (externalRoot.recursionDepth > CurrentOptions.MaxDepth &&
                        externalRoot.HasTypeInParents(item.type))
                    {
                        return false;
                    }

                    return !item.type.IsPrimitive
                           && !item.type.IsEnum
                           && !item.isMemberOpaque;
                }

                foreach (var memberItem in _members)
                {
                    if ((memberItem.isMemberUnique || memberItem.memberInfo is MethodInfo)
                        && externalRoot.HasMemberInParents(memberItem.memberInfo, includeSelf: true))
                    {
                        continue;
                    }

                    if (memberItem.isTypeOpaque)
                    {
                        continue;
                    }

                    if (!IsValid(memberItem.memberInfo))
                    {
                        continue;
                    }
                    
                    // if(externalRoot.HasRepetitionInParents(memberItem.name, memberItem.type, MAX_SAME_TYPE_AND_NAME_DEPTH, includeSelf: true))
                    // {
                    //     continue;
                    // }
                    
                    var nextItem = externalRoot.AddChild(memberItem);

                    if (!ShouldRecurse(memberItem)) continue;

                    foreach (var child in nextItem.children)
                    {
                        if(!ShouldRecurse(child)) continue;
                        
                        var childGroup = GetReflectionGroup(child.type);
                        childGroup?.AttachRecursivelyTo(child, currentDepth + 1);
                    }
                    
                    var group = GetReflectionGroup(nextItem.type);
                    group?.AttachRecursivelyTo(nextItem, currentDepth + 1);
                }
            }

            private void BuildSelfMembersIfNeeded()
            {
                if (_members != null) return;
                
                _members = new List<MemberItem>();

                bool TryCreateTemplate(MemberInfo memberInfo, out Type memberType, out MemberItem memberItem)
                {
                    memberType = GetType(memberInfo);
                    if (memberType == null
                        || IsHiddenType(memberType)
                        || IsHiddenMember(memberInfo)
                        || !IsValid(memberInfo))
                    {
                        memberItem = null;
                        return false;
                    }

                    var memberName = CurrentOptions.UseNiceNames
                        ? StringUtility.NicifyName(memberInfo.Name)
                        : memberInfo.Name;
                    // Add parameters here to path and/or name
                    var path = GetPath(memberInfo);
                    memberItem = new MemberItem(memberName, path, 0, memberInfo, null, null)
                    {
                        isTypeOpaque = IsOpaqueType(type),
                        isMemberOpaque = IsOpaqueMember(memberInfo),
                        isMemberUnique = IsUniqueMember(memberInfo),
                    };

                    if (!ApplyOverrides(memberInfo, memberItem)) return false;
                    
                    return true;
                }

                bool ApplyOverrides(MemberInfo memberInfo, MemberItem memberItem)
                {
                    if (!_overrides.TryGetValue(memberInfo, out var @override)) return true;
                    
                    if(@override.isVisible == false)
                    {
                        return false;
                    }
                        
                    memberItem.canRead = @override.canRead ?? memberItem.canRead;
                    memberItem.canWrite = @override.canWrite ?? memberItem.canWrite;
                    memberItem.isTypeOpaque = @override.isTypeOpaque ?? memberItem.isTypeOpaque;
                    memberItem.isMemberOpaque = @override.isMemberOpaque ?? memberItem.isMemberOpaque;
                    memberItem.isMemberUnique = @override.isMemberUnique ?? memberItem.isMemberUnique;

                    return true;
                }

                foreach (var member in ReflectionFactory.GetMembers(type))
                {
                    if (!TryCreateTemplate(member, out var memberType, out var memberItem)) continue;

                    _members.Add(memberItem);

                    if (!memberType.IsArray) continue;
                    
                    var elemType = memberType.GetElementType();
                    if (IsHiddenType(elemType))
                    {
                        continue;
                    }
                    
                    var elementItem = new MemberItem("[Index]",
                        Accessors.AccessorPath.ArrayPrefix,
                        0,
                        memberItem.memberInfo,
                        memberItem,
                        0,
                        elemType)
                    {
                        isTypeOpaque = IsOpaqueType(elemType),
                        isMemberUnique = IsUniqueMember(memberItem.memberInfo),
                        isMemberOpaque = IsOpaqueMember(memberItem.memberInfo),
                    };

                    if (!ApplyOverrides(memberItem.memberInfo, memberItem))
                    {
                        memberItem.RemoveChild(elementItem);
                        // _members.Add(elementItem);
                    }
                }
            }

            private string GetPath(MemberInfo member)
            {
                switch (member)
                {
                    case PropertyInfo info:
                        var indexParams = info.GetIndexParameters();
                        if (indexParams?.Length > 0)
                        {
                            return $"{info.Name}[{string.Join(",", indexParams.Select(p => p.ParameterType.Name))}]";
                        }
                        else
                        {
                            return info.Name;
                        }
                    case MethodInfo info:
                        var parameters = info.GetParameters();
                        if (parameters?.Length > 0)
                        {
                            return $"{info.Name}[{string.Join(",", parameters.Select(p => p.ParameterType.Name))}]";
                        }
                        else
                        {
                            return info.Name;
                        }
                    default: return member.Name;
                }
            }

            private bool IsUniqueMember(MemberInfo member)
            {
                return _uniqueMembers.Contains(type, member)
                    || member.GetCustomAttribute<HideMemberAttribute>()?.HowToHide == HideMemberAttribute.Hide.ShowOnlyOnce;
            }

            private bool IsOpaqueMember(MemberInfo member)
            {
                return _opaqueMembers.Contains(type, member) 
                    || member.GetCustomAttribute<HideMemberAttribute>()?.HowToHide == HideMemberAttribute.Hide.InternalsOnly;
            }

            private static bool IsOpaqueType(Type type)
            {
                return _opaqueTypes.Contains(type)
                    || type.GetCustomAttribute<HideMemberAttribute>()?.HowToHide == HideMemberAttribute.Hide.InternalsOnly;
            }

            private bool IsHiddenMember(MemberInfo member)
            {
                return _hiddenMembers.Contains(type, member)
                    || member.GetCustomAttribute<HideMemberAttribute>()?.HowToHide == HideMemberAttribute.Hide.Completely;
            }

            private static bool IsHiddenType(Type type)
            {
                return _hiddenTypes.Contains(type)
                    || type.GetCustomAttribute<HideMemberAttribute>()?.HowToHide == HideMemberAttribute.Hide.Completely;
            }

            public IEnumerable<MemberItem> GetRootChildren(Predicate<MemberItem> filter = null)
            {
                return filter != null ? ((List<MemberItem>)root.children).FindAll(filter) : root.children;
            }
            
            public IEnumerable<MemberItem> GetMembers(Predicate<MemberInfo> filter = null)
            {
                return filter != null ? AllMembers.FindAll(m => filter(m.memberInfo)) : AllMembers;
            }

            public IEnumerable<MemberItem> GetMembers(MemberTypes types, Predicate<MemberInfo> filter = null)
            {
                return filter != null
                    ? AllMembers.FindAll(m => (m.memberInfo.MemberType & types) != 0 && filter(m.memberInfo))
                    : AllMembers.FindAll(m => (m.memberInfo.MemberType & types) != 0);
            }

            public IEnumerable<MemberItem> GetMembers(Predicate<MemberItem> filter = null)
            {
                return filter != null ? AllMembers.FindAll(m => filter(m)) : AllMembers;
            }

            public IEnumerable<MemberItem> GetMembers(MemberTypes types, Predicate<MemberItem> filter = null)
            {
                return filter != null
                    ? AllMembers.FindAll(m => (m.memberInfo.MemberType & types) != 0  && filter(m))
                    : AllMembers.FindAll(m => (m.memberInfo.MemberType & types) != 0);
            }

            public IEnumerable<MemberItem> GetMembers<T>(Predicate<T> filter = null) where T : MemberInfo
            {
                return filter != null 
                    ? AllMembers.FindAll(m => m.memberInfo is T tmember && filter(tmember))
                    : AllMembers.FindAll(m => m.memberInfo is T);
            }

            private static Type GetType(MemberInfo memberInfo)
            {
                return memberInfo switch
                {
                    FieldInfo fieldInfo => fieldInfo.FieldType,
                    PropertyInfo propertyInfo => propertyInfo.PropertyType,
                    MethodInfo methodInfo => methodInfo.ReturnType,
                    EventInfo eventInfo => eventInfo.EventHandlerType,
                    _ => null
                };
            }
        }
    }

    class MembersFilter
    {
        private readonly HashSet<(Type type, MemberInfo member)> _preciseMembers = new HashSet<(Type, MemberInfo)>();
        private readonly HashSet<(Type type, MemberInfo member)> _derivedMembers = new HashSet<(Type type, MemberInfo member)>();

        public void Add(Type type, MemberInfo member, bool includeDerived)
        {
            var key = (type, member);
            if (includeDerived)
            {
                _derivedMembers.Add(key);
            }
            else
            {
                _preciseMembers.Add(key);
            }
        }

        public bool Contains(Type type, MemberInfo member)
        {
            var key = (type, member);
            if (_preciseMembers.Contains(key))
            {
                return true;
            }
            foreach(var (t, m) in _derivedMembers)
            {
                if(member.Name == m.Name && member.MemberType == m.MemberType && t.IsAssignableFrom(type))
                {
                    return true;
                }
            }
            return false;
        }
    }


    public class MemberItem
    {
        private readonly List<MemberItem> _children;
        
        internal bool isTypeOpaque;
        internal bool isMemberOpaque;
        internal bool isMemberUnique;

        public readonly string name;
        public readonly Type type;
        public readonly MemberInfo memberInfo;
        
        public bool canRead;
        public bool canWrite;

        public readonly int recursionDepth;
        public readonly MemberItem parent;
        public readonly string path;

        public readonly int? arrayElementIndex;

        public IReadOnlyList<MemberItem> children => _children;
        
        public bool HasValidChildren(Predicate<MemberItem> filter)
        {
            return filter != null ? _children.Exists(filter) : _children.Count > 0;
        }

        internal MemberItem(string name, string path, int depth, MemberInfo memberInfo, MemberItem parent, int? arrayElementIndex, Type type = null)
        {
            this.name = name;
            this.type = type ?? GetType(memberInfo);
            this.memberInfo = memberInfo;
            var (read, write) = this.type.IsArray ? (true, true) : GetReadWrite(memberInfo);

            _children = new List<MemberItem>();
            if (parent != null)
            {
                this.parent = parent;
                this.parent?._children.Add(this);
                if (!string.IsNullOrEmpty(parent.path) && !parent.path.EndsWith(ReflectionFactory.SEPARATOR))
                {
                    this.path = parent.path + ReflectionFactory.SEPARATOR + path;
                }
                else
                {
                    this.path = parent.path + path;
                }

                recursionDepth = Mathf.Max(parent.recursionDepth, depth);
                canRead = read && parent.canRead;
                canWrite = write && parent.canWrite;
            }
            else
            {
                this.parent = null;
                this.path = path;
                this.recursionDepth = depth;
                canRead = read;
                canWrite = write;
            }
        }

        private static Type GetType(MemberInfo memberInfo)
        {
            return memberInfo switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                MethodInfo methodInfo => methodInfo.ReturnType,
                EventInfo eventInfo => eventInfo.EventHandlerType,
                Type type => type,
                _ => null
            };
        }

        private static (bool canRead, bool canWrite) GetReadWrite(MemberInfo memberInfo)
        {
            return memberInfo switch
            {
                FieldInfo fieldInfo => (true, !fieldInfo.IsInitOnly),
                PropertyInfo propertyInfo => (propertyInfo.CanRead, propertyInfo.CanWrite),
                MethodInfo methodInfo => (methodInfo.ReturnType != typeof(void), true),
                EventInfo eventInfo => (true, true),
                _ => (true, true),
            };
        }

        internal MemberItem AddChild(MemberItem item, bool removeCommonPath = false)
        {
            var depth = HasTypeInParents(item.type) ? recursionDepth + 1 : recursionDepth;
            var childPath = item.path;
            if (removeCommonPath)
            {
                // Remove common path, for example
                // if parent path is "A/B" and child path is "B/C", the result final path will be "A/B/C"
                var lastSeparator = path.LastIndexOf(ReflectionFactory.SEPARATOR);
                var lastPart = lastSeparator >= 0 ? path.Substring(lastSeparator + 1) : path;
                childPath = childPath.RemoveAtStart(lastPart).TrimStart(ReflectionFactory.SEPARATOR);
            }
            var child = new MemberItem(item.name,
                childPath,
                depth,
                item.memberInfo,
                this,
                item.arrayElementIndex,
                item.type)
            {
                canRead = item.canRead,
                canWrite = item.canWrite,
                isTypeOpaque = item.isTypeOpaque,
                isMemberOpaque = item.isMemberOpaque,
                isMemberUnique = item.isMemberUnique,
            };
            foreach (var itemChild in item._children)
            {
                child.AddChild(itemChild, true);
            }
            return child;
        }
        
        internal bool HasMemberInParents(MemberInfo member, bool includeSelf = true)
        {
            var item = includeSelf ? this : parent;
            while(item != null)
            {
                if(item.memberInfo.Name == member.Name 
                   && item.memberInfo.MemberType == member.MemberType
                   && item.type == member.GetMemberType())
                {
                    return true;
                }
                item = item.parent;
            }

            return false;
        }

        internal bool HasTypeInParents(Type type, bool includeSelf = true)
        {
            var item = includeSelf ? this : parent;
            while (item != null)
            {
                if (item.type == type)
                {
                    return true;
                }
                item = item.parent;
            }

            return false;
        }
        
        internal bool HasRepetitionInParents(string name, Type type, int maxAllowedDepth, bool includeSelf = true)
        {
            var item = includeSelf ? this : parent;
            while (item != null)
            {
                if (item.type == type && item.name == name)
                {
                    maxAllowedDepth--;
                    if (maxAllowedDepth <= 0)
                    {
                        return true;
                    }
                }
                item = item.parent;
            }

            return false;
        }

        internal bool InTrueInAnyParent(Predicate<MemberItem> predicate, bool includeSelf = true)
        {
            var item = includeSelf ? this : parent;
            while (item != null)
            {
                if (predicate(item))
                {
                    return true;
                }
                item = item.parent;
            }

            return false;
        }

        internal void FlatHierarchy(List<MemberItem> list, int maxDepth)
        {
            list.Add(this);
            foreach(var child in _children)
            {
                if (child.recursionDepth <= maxDepth)
                {
                    child.FlatHierarchy(list, maxDepth);
                }
            }
        }

        public void RemoveChild(MemberItem child)
        {
            _children.Remove(child);
        }
    }
}