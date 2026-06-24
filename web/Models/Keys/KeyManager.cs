using System;
using System.Collections.Generic;

namespace Viper.Models.Keys;

public partial class KeyManager
{
    public int KeyManagerId { get; set; }

    public int KeyId { get; set; }

    public string ManagerId { get; set; } = null!;
}
