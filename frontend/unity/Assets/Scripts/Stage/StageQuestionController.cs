using System;
using System.Collections.Generic;

public class StageQuestionController<TQuestion> where TQuestion : class
{
    private readonly List<TQuestion> _questions = new List<TQuestion>();
    private int _currentIndex = -1;

    public IReadOnlyList<TQuestion> Questions => _questions;
    public int Count => _questions.Count;
    public int CurrentIndex => _currentIndex;
    public int CurrentNumber => _currentIndex >= 0 ? _currentIndex + 1 : 0;
    public TQuestion Current => (_currentIndex >= 0 && _currentIndex < _questions.Count) ? _questions[_currentIndex] : null;

    public void Clear()
    {
        _questions.Clear();
        _currentIndex = -1;
    }

    public void SetQuestions(IEnumerable<TQuestion> questions)
    {
        _questions.Clear();
        if (questions != null)
        {
            foreach (var q in questions)
            {
                if (q != null)
                    _questions.Add(q);
            }
        }
        _currentIndex = _questions.Count > 0 ? 0 : -1;
    }

    public bool SetCurrentQuestionNumber(int questionNumber)
    {
        int index = questionNumber - 1;
        if (index < 0 || index >= _questions.Count)
            return false;
        _currentIndex = index;
        return true;
    }

    public bool TryGetQuestionByNumber(int questionNumber, out TQuestion question)
    {
        int index = questionNumber - 1;
        if (index >= 0 && index < _questions.Count)
        {
            question = _questions[index];
            return true;
        }

        question = null;
        return false;
    }

    public TQuestion GetQuestionByNumber(int questionNumber)
    {
        return TryGetQuestionByNumber(questionNumber, out var question) ? question : null;
    }

    public IEnumerable<TQuestion> GetAllQuestions()
    {
        return _questions;
    }
}
