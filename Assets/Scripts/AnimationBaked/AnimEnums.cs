public enum Model : short { FylingDemonPBRDefault, CrabMonsterPBRDefault }
public static class ModelSize { public const short Size = 2; }

public enum FylingDemonPBRDefault : byte { Idle, Move, Die, Attack }
public static class FylingDemonPBRDefaultSize { public const byte Size = 4; }
public enum CrabMonsterPBRDefault : byte { Idle, Attack, Die, Move }
public static class CrabMonsterPBRDefaultSize { public const byte Size = 4; }
