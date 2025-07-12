using System.Text;
using System.Threading;
using ZergRush.CodeGen;

[GenTask(GenTaskFlags.SimpleDataPack), GenInLocalFolder]
[Serializable]
public class ConcurrentBitset
{
    private long[] words;
    private int size;

    public ConcurrentBitset(int size, bool val)
    {
        words = new long[size >> 6];
        this.size = size;
        if (val)
        {
            for (int i = 0; i < size; i++)
            {
                words[i] = ~((long)0);
            }
        }
    }
    
    public bool this[int index]
    {
        get
        {
            var word = index >> 6;
            return (Interlocked.Read(ref words[word]) & (1 << (index & 63))) > 0;
        }
        set
        {
            var word = index >> 6;
            var mask = 1L << (index & 63);

            long current, target;
            do
            {
                current = Interlocked.Read(ref words[word]);
                target = value ? (current | mask) : (current & ~mask);
            } while (Interlocked.CompareExchange(ref words[word], target, current) != current);
        }
    }
    
    public bool IsSame(ConcurrentBitset other)
    {
        if (size != other.size) return false;
        for (int i = 0; i < words.Length; i++)
        {
            if (Interlocked.Read(ref words[i]) != Interlocked.Read(ref other.words[i]))
            {
                return false;
            }
        }
        return true;
    }
    
    public string ToString()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < size; i++)
        {
            sb.Append(this[i] ? '1' : '0');
        }
        return sb.ToString();
    }
}
