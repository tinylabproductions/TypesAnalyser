using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using com.tinylabproductions.TLPLib.Extensions;
using com.tinylabproductions.TLPLib.Functional;
using Mono.Cecil;

namespace TypesAnalyser {
  public interface IEntryPoint {
    IEnumerable<MethodDefinition> entryMethods { get; }
  }

  public static class EntryPoint {
    public static Option<IEntryPoint> create(TypeDefinition type) { return UnityEntryPoint.create(type); }

    public static bool canBeEntryPoint(TypeDefinition type) { return !type.IsGenericInstance; }
    public static bool canBeEntryPoint(MethodDefinition type) { return !type.IsGenericInstance; }
  }

  public class UnityEntryPoint : IEntryPoint {
    public readonly ImmutableHashSet<string> entryPointMethods = new[] {
      "Awake",
      "FixedUpdate",
      "LateUpdate",
      "OnAnimatorIK",
      "OnAnimatorMove",
      "OnApplicationFocus",
      "OnApplicationPause",
      "OnApplicationQuit",
      "OnAudioFilterRead",
      "OnBecameInvisible",
      "OnBecameVisible",
      "OnCollisionEnter",
      "OnCollisionEnter2D",
      "OnCollisionExit",
      "OnCollisionExit2D",
      "OnCollisionStay",
      "OnCollisionStay2D",
      "OnConnectedToServer",
      "OnControllerColliderHit",
      "OnDestroy",
      "OnDisable",
      "OnDisconnectedFromServer",
      "OnDrawGizmos",
      "OnDrawGizmosSelected",
      "OnEnable",
      "OnFailedToConnect",
      "OnFailedToConnectToMasterServer",
      "OnGUI",
      "OnJointBreak",
      "OnLevelWasLoaded",
      "OnMasterServerEvent",
      "OnMouseDown",
      "OnMouseDrag",
      "OnMouseEnter",
      "OnMouseExit",
      "OnMouseOver",
      "OnMouseUp",
      "OnMouseUpAsButton",
      "OnNetworkInstantiate",
      "OnParticleCollision",
      "OnPlayerConnected",
      "OnPlayerDisconnected",
      "OnPostRender",
      "OnPreCull",
      "OnPreRender",
      "OnRenderImage",
      "OnRenderObject",
      "OnSerializeNetworkView",
      "OnServerInitialized",
      "OnTransformChildrenChanged",
      "OnTransformParentChanged",
      "OnTriggerEnter",
      "OnTriggerEnter2D",
      "OnTriggerExit",
      "OnTriggerExit2D",
      "OnTriggerStay",
      "OnTriggerStay2D",
      "OnValidate",
      "OnWillRenderObject",
      "Reset",
      "Start",
      "Update",
    }.ToImmutableHashSet();

    public readonly ExpandedType type;

    public UnityEntryPoint(ExpandedType type) {
      this.type = type;
    }

    public override string ToString() {
      return $"UnityEntryPoint[{type}]";
    }

    static bool isEntryPoint(TypeDefinition type) {
      if (type.BaseType == null) return false;
      else {
        return EntryPoint.canBeEntryPoint(type) && (
                 type.BaseType.FullName == "UnityEngine.MonoBehaviour"
                 || isEntryPoint(type.BaseType.Resolve())
               );
      }
    }

    public static Option<IEntryPoint> create(TypeDefinition type) {
      return isEntryPoint(type).opt<IEntryPoint>(() => new UnityEntryPoint(
        ExpandedType.create(type, ExpandedType.EMPTY_GENERIC_LOOKUP)
      ));
    }

    public IEnumerable<MethodDefinition> entryMethods {
      get { return type.definition.Methods.Where(m => 
        entryPointMethods.Contains(m.Name) && EntryPoint.canBeEntryPoint(m) && !m.IsStatic
      ); }
    }
  }
}
