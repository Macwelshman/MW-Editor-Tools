using System;
namespace MW.EditorTools.Modules.BulldozerRecovery {
internal sealed partial class BulldozerTrace {
    // Isolate actual forest matching from Unity; placement includes root prefab identity.
    class NestedNode {internal object Prefab;internal int Position;}
    static bool SameNestedPlacement(NestedNode a,NestedNode b)=>ReferenceEquals(a.Prefab,b.Prefab)&&a.Position==b.Position;
    static NestedNode[] Group(object prefab,int position)=>new[]{new NestedNode{Prefab=prefab,Position=position}};
    static void MixedTypeChecks(){
        Check(BoundPropPolicy.Accept(new[]{-1},new[]{0},new[]{new object()},new[]{true}));
        Check(!NestedPropPolicy.Accept(new[]{-1},new[]{0},new[]{new object()},new[]{true}));
        Check(!BoundPropPolicy.Accept(new[]{0},new[]{0},new[]{new object()},new[]{true}));
        Check(!BoundPropPolicy.Accept(new[]{-1},new[]{1},new[]{new object()},new[]{true}));
        Check(!BoundPropPolicy.Accept(new[]{-1},new[]{0},new object[]{null},new[]{true}));
        Check(!BoundPropPolicy.Accept(new[]{-1},new[]{0},new[]{new object()},new[]{false}));
        Check(!BoundPropPolicy.Accept(new[]{-1},Array.Empty<int>(),new[]{new object()},new[]{true}));
        Check(!BoundPropPolicy.Accept(new[]{-1},new[]{0},Array.Empty<object>(),new[]{true}));
        Check(!BoundPropPolicy.Accept(new[]{-1},new[]{0},new[]{new object()},Array.Empty<bool>()));
        Check(!BoundPropPolicy.Accept(null,new[]{0},new[]{new object()},new[]{true}));
        Check(BoundPropPolicy.Accept(new[]{-1,0},new[]{0,1},new[]{new object(),new object()},new[]{true,true}));
        Check(!BoundPropPolicy.Accept(new[]{-1,0},new[]{0,1},new[]{new object(),new object()},new[]{true,false}));
        var first=new object();var second=new object();
        var onlySimple=BoundPropSelection.Select(new[]{first,second},_=>false);
        Check(onlySimple.Length==2&&ReferenceEquals(onlySimple[0],first)&&ReferenceEquals(onlySimple[1],second));
        var nestedFirst=BoundPropSelection.Select(new[]{first,second},value=>ReferenceEquals(value,second));
        Check(nestedFirst.Length==1&&ReferenceEquals(nestedFirst[0],second));
        Check(BoundPropSelection.Select<object>(null,_=>false).Length==0);
        Check(BoundPropSelection.Select(Array.Empty<object>(),_=>false).Length==0);
        var simpleSource=new[]{first};var independent=BoundPropSelection.Select(simpleSource,_=>false);simpleSource[0]=second;
        Check(ReferenceEquals(independent[0],first)&&!ReferenceEquals(simpleSource,independent));
        Check(BoundPropSelection.Select(new[]{first,second},_=>true).Length==2);
        var source=new[]{first,second};var frozen=NestedRootTypes.Copy(source);source[0]=new object();
        Check(ReferenceEquals(frozen[0],first)&&ReferenceEquals(frozen[1],second));
        Check(!ReferenceEquals(frozen,source));
        foreach(var invalid in new object[][]{null,Array.Empty<object>(),new[]{first,first},new object[]{first,null},new[]{first,second,new object(),new object(),new object()}}){
            bool refused=false;try{NestedRootTypes.Copy(invalid);}catch(NotSupportedException){refused=true;}Check(refused);
        }
        Check(NestedRootTypes.Copy(new[]{first}).Length==1);
        Check(NestedRootTypes.Copy(new[]{first,second,new object(),new object()}).Length==4);
        var a=Group(first,10);var b=Group(second,20);
        var ordered=MatchForest(new[]{a,b},new[]{b,a});Check(ReferenceEquals(ordered[0],a)&&ReferenceEquals(ordered[1],b));
        Check(MatchForest(new[]{a,b},new[]{a})==null);
        Check(MatchForest(new[]{a},new[]{a,b})==null);
        Check(MatchForest(new[]{a,b},new[]{a,Group(first,20)})==null);
        Check(MatchForest(new[]{a,b},new[]{a,Group(second,21)})==null);
        var colocated=Group(second,10);ordered=MatchForest(new[]{a,colocated},new[]{colocated,a});
        Check(ReferenceEquals(ordered[0],a)&&ReferenceEquals(ordered[1],colocated));
        Check(MatchForest(new[]{a,Group(first,10)},new[]{a,Group(first,10)})==null);
        Check(MatchForest(Array.Empty<NestedNode[]>(),Array.Empty<NestedNode[]>()).Length==0);
        Check(MatchForest(new[]{a},new[]{a})[0]==a);
        // After deleting every instance of one type, the frozen set still includes it.
        var remaining=MatchForest(new[]{b},new[]{b});Check(remaining.Length==1&&ReferenceEquals(frozen[0],first));
        ordered=MatchForest(new[]{a,b},new[]{b,a});Check(ordered.Length==2&&ReferenceEquals(ordered[0][0].Prefab,first));
    }
}
}
