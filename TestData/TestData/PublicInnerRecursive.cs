namespace TestData {
  class PublicInnerRecursive<A> {
    public class Node {
      internal A value;
      internal Node prev;
    }

    Node head;

    public PublicInnerRecursive(A value) {
      head = new Node {value = value};
      head.prev = head;
    }

    public A get { get { return head.value; } }
  }
}
