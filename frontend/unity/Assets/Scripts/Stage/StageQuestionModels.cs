using System;
using System.Collections.Generic;

public static class StageQuestionModels
{
    [Serializable]
    public class OptionDto
    {
        public int id;
        public string value;
        public string unicode;
    }

    [Serializable]
    public class QuestionDto
    {
        public int id;
        public int questionId;
        public int phonemeId;
        public string problemWord;
        public string value;
        public string unicode;
        public string voiceUrl;
        public string imageUrl;
        public List<OptionDto> options;
    }
}
