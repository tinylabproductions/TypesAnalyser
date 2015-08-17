namespace TestData {
  abstract class ACNonGeneric {
    public abstract int identity(int value);
  }

  class ACNormalIdentity : ACNonGeneric {
    public override int identity(int value) { return value; }
  }

  class ACLyingIdentity : ACNonGeneric {
    public override int identity(int value) { return 0; }
  }

  class ACNeverCalledIdentity : ACNonGeneric {
    public override int identity(int value) { return value; }
  }

  abstract class ACNonGeneric2 {
    public abstract int identity(int value);
  }

  class ACRandomIdentity : ACNonGeneric2 {
    public override int identity(int value) {
      var id = value < 0 ? (ACNonGeneric) new ACNormalIdentity() : new ACLyingIdentity();
      return id.identity(value);
    }
  }

  class ACCircularIdentity : ACNonGeneric {
    public override int identity(int value) {
      if (value < 0) {
        ACNonGeneric2 iface = new ACCircularIdentity2();
        return iface.identity(value);
      }
      else return value;
    }
  }

  class ACCircularIdentity2 : ACNonGeneric2 {
    public override int identity(int value) {
      if (value > 0) {
        ACNonGeneric iface = new ACCircularIdentity();
        return iface.identity(value);
      }
      else return value;
    }
  }

  class ACCircularIfaceIdentity : ACNonGeneric {
    public override int identity(int value) {
      if (value > 0) {
        INonGeneric iface = new CircularAbstractClassIdentity();
        return iface.identity(value);
      }
      else return value;
    }
  }
}
