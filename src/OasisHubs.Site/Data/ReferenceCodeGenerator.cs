using System.Security.Cryptography;
using System.Text;

namespace OasisHubs.Site.Data;
/*
 * source: https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings
 */
public static class ReferenceCodeGenerator
{
   private static readonly char[] _chars =
      "ABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray();

   public static string GetUniqueKey(int size = 8)
   {
      var data = new byte[4 * size];
      using var crypto = RandomNumberGenerator.Create();
      crypto.GetBytes(data);
      var result = new StringBuilder(size);
      result.Append("OAS-");
      for (var i = 0; i < size; i++)
      {
         var rnd = BitConverter.ToUInt32(data, i * 4);
         var idx = rnd % _chars.Length;
         result.Append(_chars[idx]);
      }

      return result.ToString();
   }
}
