﻿namespace SharpAI.Prompts
{
    /// <summary>
    /// Defines common chat/prompt formats used by different language models.
    /// Each format has specific tokens and structure that the model was trained to recognize.
    /// </summary>
    public enum ChatFormat
    {
        /// <summary>
        /// Simple role-based format using "role: content" structure.
        /// Compatible with many base models and general-purpose LLMs.
        /// </summary>
        /// <example>
        /// system: You are a helpful assistant.
        /// user: What is 2+2?
        /// assistant: 4
        /// user: Why?
        /// assistant:
        /// </example>
        Simple,

        /// <summary>
        /// ChatML (Chat Markup Language) format used by OpenAI models and many modern LLMs.
        /// Uses special tokens: &lt;|im_start|&gt;, &lt;|im_end|&gt;
        /// </summary>
        /// <example>
        /// &lt;|im_start|&gt;system
        /// You are a helpful assistant.&lt;|im_end|&gt;
        /// &lt;|im_start|&gt;user
        /// What is 2+2?&lt;|im_end|&gt;
        /// &lt;|im_start|&gt;assistant
        /// 4&lt;|im_end|&gt;
        /// &lt;|im_start|&gt;user
        /// Why?&lt;|im_end|&gt;
        /// &lt;|im_start|&gt;assistant
        /// </example>
        ChatML,

        /// <summary>
        /// Llama 2 Chat format with instruction tags.
        /// Uses [INST], [/INST] tags and &lt;&lt;SYS&gt;&gt; for system prompts.
        /// </summary>
        /// <example>
        /// &lt;s&gt;[INST] &lt;&lt;SYS&gt;&gt;
        /// You are a helpful assistant.
        /// &lt;&lt;/SYS&gt;&gt;
        /// 
        /// What is 2+2? [/INST] 4 &lt;/s&gt;&lt;s&gt;[INST] Why? [/INST]
        /// </example>
        Llama2,

        /// <summary>
        /// Llama 3 format with header-based structure.
        /// Uses header IDs and special tokens for role demarcation.
        /// </summary>
        /// <example>
        /// &lt;|begin_of_text|&gt;&lt;|start_header_id|&gt;system&lt;|end_header_id|&gt;
        /// 
        /// You are a helpful assistant.&lt;|eot_id|&gt;&lt;|start_header_id|&gt;user&lt;|end_header_id|&gt;
        /// 
        /// What is 2+2?&lt;|eot_id|&gt;&lt;|start_header_id|&gt;assistant&lt;|end_header_id|&gt;
        /// </example>
        Llama3,

        /// <summary>
        /// Alpaca instruction format using ### markers.
        /// Commonly used by instruction-tuned models based on LLaMA.
        /// </summary>
        /// <example>
        /// ### System:
        /// You are a helpful assistant.
        /// 
        /// ### Instruction:
        /// What is 2+2?
        /// 
        /// ### Response:
        /// 4
        /// 
        /// ### Instruction:
        /// Why?
        /// 
        /// ### Response:
        /// </example>
        Alpaca,

        /// <summary>
        /// Mistral/Mixtral instruction format.
        /// Uses [INST] tags similar to Llama 2 but with simpler structure.
        /// </summary>
        /// <example>
        /// &lt;s&gt;[INST] What is 2+2? [/INST] 4&lt;/s&gt;[INST] Why? [/INST]
        /// </example>
        Mistral,

        /// <summary>
        /// Human/Assistant conversational format.
        /// Used by Anthropic's Claude and some other conversational models.
        /// </summary>
        /// <example>
        /// Human: What is 2+2?
        /// 
        /// Assistant: 4
        /// 
        /// Human: Why?
        /// 
        /// Assistant:
        /// </example>
        HumanAssistant,

        /// <summary>
        /// Zephyr model format with role tags.
        /// Uses &lt;|role|&gt; tags with &lt;/s&gt; as separators.
        /// </summary>
        /// <example>
        /// &lt;|system|&gt;
        /// You are a helpful assistant.&lt;/s&gt;
        /// &lt;|user|&gt;
        /// What is 2+2?&lt;/s&gt;
        /// &lt;|assistant|&gt;
        /// 4&lt;/s&gt;
        /// &lt;|user|&gt;
        /// Why?&lt;/s&gt;
        /// &lt;|assistant|&gt;
        /// </example>
        Zephyr,

        /// <summary>
        /// Phi model format used by Microsoft's Phi series.
        /// Uses specific instruction and response markers.
        /// </summary>
        /// <example>
        /// Instruct: What is 2+2?
        /// Output: 4
        /// Instruct: Why?
        /// Output:
        /// </example>
        Phi,

        /// <summary>
        /// DeepSeek model format.
        /// Similar to simple format but with specific markers.
        /// </summary>
        /// <example>
        /// User: What is 2+2?
        /// 
        /// Assistant: 4
        /// 
        /// User: Why?
        /// 
        /// Assistant:
        /// </example>
        DeepSeek
    }
}
