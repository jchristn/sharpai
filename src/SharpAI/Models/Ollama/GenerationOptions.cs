namespace SharpAI.Models.Ollama
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    /// <summary>
    /// Ollama generation options.
    /// </summary>
    public class GenerationOptions
    {
        #region Public-Members

        /// <summary>
        /// Number of tokens from prompt to keep when context fills.
        /// </summary>
        [JsonPropertyName("num_keep")]
        public int? NumKeep
        {
            get
            {
                return _NumKeep;
            }
            set
            {
                if (value.HasValue && value.Value < 0) throw new ArgumentOutOfRangeException(nameof(NumKeep));
                _NumKeep = value;
            }
        }

        /// <summary>
        /// Random seed for reproducible outputs (-1 = random).
        /// </summary>
        [JsonPropertyName("seed")]
        public int? Seed
        {
            get
            {
                return _Seed;
            }
            set
            {
                if (value.HasValue && value.Value < -1) throw new ArgumentOutOfRangeException(nameof(Seed));
                _Seed = value;
            }
        }

        /// <summary>
        /// Maximum tokens to generate (-1 = unlimited, -2 = fill context).
        /// </summary>
        [JsonPropertyName("num_predict")]
        public int? NumPredict
        {
            get
            {
                return _NumPredict;
            }
            set
            {
                if (value.HasValue && value.Value < -2) throw new ArgumentOutOfRangeException(nameof(NumPredict));
                _NumPredict = value;
            }
        }

        /// <summary>
        /// Limits vocabulary to top K most likely tokens.
        /// </summary>
        [JsonPropertyName("top_k")]
        public int? TopK
        {
            get
            {
                return _TopK;
            }
            set
            {
                if (value.HasValue && (value.Value < 1 || value.Value > 100)) throw new ArgumentOutOfRangeException(nameof(TopK));
                _TopK = value;
            }
        }

        /// <summary>
        /// Nucleus sampling - cumulative probability cutoff.
        /// </summary>
        [JsonPropertyName("top_p")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? TopP
        {
            get
            {
                return _TopP;
            }
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 1.0f)) throw new ArgumentOutOfRangeException(nameof(TopP));
                _TopP = value;
            }
        }

        /// <summary>
        /// Minimum probability threshold for token selection.
        /// </summary>
        [JsonPropertyName("min_p")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? MinP
        {
            get
            {
                return _MinP;
            }
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 1.0f)) throw new ArgumentOutOfRangeException(nameof(MinP));
                _MinP = value;
            }
        }

        /// <summary>
        /// Tail-free sampling parameter.
        /// </summary>
        [JsonPropertyName("tfs_z")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? TfsZ
        {
            get
            {
                return _TfsZ;
            }
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 1.0f)) throw new ArgumentOutOfRangeException(nameof(TfsZ));
                _TfsZ = value;
            }
        }

        /// <summary>
        /// Typical sampling parameter.
        /// </summary>
        [JsonPropertyName("typical_p")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? TypicalP
        {
            get
            {
                return _TypicalP;
            }
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 1.0f)) throw new ArgumentOutOfRangeException(nameof(TypicalP));
                _TypicalP = value;
            }
        }

        /// <summary>
        /// How many tokens to consider for repetition penalty (-1 = num_ctx).
        /// </summary>
        [JsonPropertyName("repeat_last_n")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? RepeatLastN
        {
            get
            {
                return _RepeatLastN;
            }
            set
            {
                if (value.HasValue && value.Value < -1) throw new ArgumentOutOfRangeException(nameof(RepeatLastN));
                _RepeatLastN = value;
            }
        }

        /// <summary>
        /// Controls randomness. Lower = more deterministic, higher = more creative.
        /// </summary>
        [JsonPropertyName("temperature")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? Temperature
        {
            get
            {
                return _Temperature;
            }
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 2.0f)) throw new ArgumentOutOfRangeException(nameof(Temperature));
                _Temperature = value;
            }
        }

        /// <summary>
        /// Penalty for repeating tokens (1.0 = no penalty).
        /// </summary>
        [JsonPropertyName("repeat_penalty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? RepeatPenalty
        {
            get
            {
                return _RepeatPenalty;
            }
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 2.0f)) throw new ArgumentOutOfRangeException(nameof(RepeatPenalty));
                _RepeatPenalty = value;
            }
        }

        /// <summary>
        /// Penalty for token presence (positive = discourage).
        /// </summary>
        [JsonPropertyName("presence_penalty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? PresencePenalty
        {
            get
            {
                return _PresencePenalty;
            }
            set
            {
                if (value.HasValue && (value.Value < -2.0f || value.Value > 2.0f)) throw new ArgumentOutOfRangeException(nameof(PresencePenalty));
                _PresencePenalty = value;
            }
        }

        /// <summary>
        /// Penalty based on token frequency.
        /// </summary>
        [JsonPropertyName("frequency_penalty")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? FrequencyPenalty
        {
            get
            {
                return _FrequencyPenalty;
            }
            set
            {
                if (value.HasValue && (value.Value < -2.0f || value.Value > 2.0f)) throw new ArgumentOutOfRangeException(nameof(FrequencyPenalty));
                _FrequencyPenalty = value;
            }
        }

        /// <summary>
        /// Mirostat algorithm (0=disabled, 1=v1, 2=v2).
        /// </summary>
        [JsonPropertyName("mirostat")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? Mirostat
        {
            get
            {
                return _Mirostat;
            }
            set
            {
                if (value.HasValue && (value.Value < 0 || value.Value > 2)) throw new ArgumentOutOfRangeException(nameof(Mirostat));
                _Mirostat = value;
            }
        }

        /// <summary>
        /// Mirostat target entropy.
        /// </summary>
        [JsonPropertyName("mirostat_tau")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? MirostatTau
        {
            get
            {
                return _MirostatTau;
            }
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 10.0f)) throw new ArgumentOutOfRangeException(nameof(MirostatTau));
                _MirostatTau = value;
            }
        }

        /// <summary>
        /// Mirostat learning rate.
        /// </summary>
        [JsonPropertyName("mirostat_eta")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public float? MirostatEta
        {
            get
            {
                return _MirostatEta;
            }
            set
            {
                if (value.HasValue && (value.Value < 0.0f || value.Value > 1.0f)) throw new ArgumentOutOfRangeException(nameof(MirostatEta));
                _MirostatEta = value;
            }
        }

        /// <summary>
        /// Whether to penalize newline tokens.
        /// </summary>
        [JsonPropertyName("penalize_newline")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? PenalizeNewline
        {
            get
            {
                return _PenalizeNewline;
            }
            set
            {
                _PenalizeNewline = value;
            }
        }

        /// <summary>
        /// Enable NUMA optimization.
        /// </summary>
        [JsonPropertyName("numa")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? Numa
        {
            get
            {
                return _Numa;
            }
            set
            {
                _Numa = value;
            }
        }

        /// <summary>
        /// Context window size (model dependent).
        /// </summary>
        [JsonPropertyName("num_ctx")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumCtx
        {
            get
            {
                return _NumCtx;
            }
            set
            {
                if (value.HasValue && (value.Value < 1 || value.Value > 131072)) throw new ArgumentOutOfRangeException(nameof(NumCtx));
                _NumCtx = value;
            }
        }

        /// <summary>
        /// Batch size for prompt processing.
        /// </summary>
        [JsonPropertyName("num_batch")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumBatch
        {
            get
            {
                return _NumBatch;
            }
            set
            {
                if (value.HasValue && (value.Value < 1 || value.Value > 2048)) throw new ArgumentOutOfRangeException(nameof(NumBatch));
                _NumBatch = value;
            }
        }

        /// <summary>
        /// Number of GPU layers to use (-1 = auto).
        /// </summary>
        [JsonPropertyName("num_gpu")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumGpu
        {
            get
            {
                return _NumGpu;
            }
            set
            {
                if (value.HasValue && (value.Value < -1 || value.Value > 8)) throw new ArgumentOutOfRangeException(nameof(NumGpu));
                _NumGpu = value;
            }
        }

        /// <summary>
        /// Primary GPU device ID.
        /// </summary>
        [JsonPropertyName("main_gpu")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? MainGpu
        {
            get
            {
                return _MainGpu;
            }
            set
            {
                if (value.HasValue && (value.Value < 0 || value.Value > 7)) throw new ArgumentOutOfRangeException(nameof(MainGpu));
                _MainGpu = value;
            }
        }

        /// <summary>
        /// Reduce VRAM usage (slower).
        /// </summary>
        [JsonPropertyName("low_vram")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? LowVram
        {
            get
            {
                return _LowVram;
            }
            set
            {
                _LowVram = value;
            }
        }

        /// <summary>
        /// Use 16-bit floats for key/value cache.
        /// </summary>
        [JsonPropertyName("f16_kv")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? F16Kv
        {
            get
            {
                return _F16Kv;
            }
            set
            {
                _F16Kv = value;
            }
        }

        /// <summary>
        /// Load vocabulary only (for embeddings).
        /// </summary>
        [JsonPropertyName("vocab_only")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? VocabOnly
        {
            get
            {
                return _VocabOnly;
            }
            set
            {
                _VocabOnly = value;
            }
        }

        /// <summary>
        /// Use memory mapping for model loading.
        /// </summary>
        [JsonPropertyName("use_mmap")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? UseMmap
        {
            get
            {
                return _UseMmap;
            }
            set
            {
                _UseMmap = value;
            }
        }

        /// <summary>
        /// Lock model in memory (prevents swapping).
        /// </summary>
        [JsonPropertyName("use_mlock")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public bool? UseMlock
        {
            get
            {
                return _UseMlock;
            }
            set
            {
                _UseMlock = value;
            }
        }

        /// <summary>
        /// Number of CPU threads to use.
        /// </summary>
        [JsonPropertyName("num_thread")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? NumThread
        {
            get
            {
                return _NumThread;
            }
            set
            {
                if (value.HasValue && (value.Value < 1 || value.Value > 256)) throw new ArgumentOutOfRangeException(nameof(NumThread));
                _NumThread = value;
            }
        }

        #endregion

        #region Private-Members

        private int? _NumKeep = null;
        private int? _Seed = null;
        private int? _NumPredict = null;
        private int? _TopK = null;
        private float? _TopP = null;
        private float? _MinP = null;
        private float? _TfsZ = null;
        private float? _TypicalP = null;
        private int? _RepeatLastN = null;
        private float? _Temperature = null;
        private float? _RepeatPenalty = null;
        private float? _PresencePenalty = null;
        private float? _FrequencyPenalty = null;
        private int? _Mirostat = null;
        private float? _MirostatTau = null;
        private float? _MirostatEta = null;
        private bool? _PenalizeNewline = null;
        private bool? _Numa = null;
        private int? _NumCtx = null;
        private int? _NumBatch = null;
        private int? _NumGpu = null;
        private int? _MainGpu = null;
        private bool? _LowVram = null;
        private bool? _F16Kv = null;
        private bool? _VocabOnly = null;
        private bool? _UseMmap = null;
        private bool? _UseMlock = null;
        private int? _NumThread = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Ollama generation options.
        /// </summary>
        public GenerationOptions()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}