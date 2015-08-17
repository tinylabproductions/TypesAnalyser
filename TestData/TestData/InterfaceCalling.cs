namespace TestData {
  interface INonGeneric {
    int identity(int value);
  }

  class NormalIdentity : INonGeneric {
    public int identity(int value) { return value; }
  }

  class LyingIdentity : INonGeneric {
    public int identity(int value) { return 0; }
  }

  class NeverCalledIdentity : INonGeneric {
    public int identity(int value) { return value; }
  }

  interface INonGeneric2 {
    int identity(int value);
  }

  class RandomIdentity : INonGeneric2 {
    public int identity(int value) {
      var id = value < 0 ? (INonGeneric) new NormalIdentity() : new LyingIdentity();
      return id.identity(value);
    }
  }

  class CircularIdentity : INonGeneric {
    public int identity(int value) {
      if (value < 0) {
        INonGeneric2 iface = new CircularIdentity2();
        return iface.identity(value);
      }
      else return value;
    }
  }

  class CircularIdentity2 : INonGeneric2 {
    public int identity(int value) {
      if (value > 0) {
        INonGeneric iface = new CircularIdentity();
        return iface.identity(value);
      }
      else return value;
    }
  }
}
