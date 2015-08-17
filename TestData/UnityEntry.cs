using TestData;
using UnityEngine;

class UnityEntry : MonoBehaviour {
  void Start() {
    var w0 = Tuple1.a('3');
    Debug.Log(w0.value);
    var w2 = w0.add("5");
    Debug.Log(w2._1 + w2._2);

    var r0 = Recursive.a(3);
    Debug.Log(r0.value);
    var r1 = r0.wrap();
    Debug.Log(r1.value.value);
    var r2 = r1.wrap();
    Debug.Log(r2.value.value.value);
    var r21 = r0.wrap2(3);
    Debug.Log(r21.value._1 + r21.value._2);
    var r22 = r21.wrap2(5);
    Debug.Log(r22.value._1._1 + r22.value._1._2 + r22.value._2);
  }
}
