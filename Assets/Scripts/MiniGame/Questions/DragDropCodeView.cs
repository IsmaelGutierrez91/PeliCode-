using System;
using System.Collections.Generic;
using System.Linq;
using PeliCode.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeliCode.MiniGame.Questions
{
    public class DragDropCodeView : MonoBehaviour, IQuestionView
    {
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private Transform blocksContainer;
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private DragDropBlock blockPrefab;
        [SerializeField] private DragDropSlot slotPrefab;
        [SerializeField] private Button checkButton;

        public event Action<bool> OnAnswered;
        public GameObject GameObject => gameObject;

        private QuestionData _question;
        private readonly List<DragDropSlot> _slots = new List<DragDropSlot>();
        private readonly List<DragDropBlock> _blocks = new List<DragDropBlock>();

        public void Setup(QuestionData question)
        {
            _question = question;
            questionText.text = question.questionText;

            ClearChildren(blocksContainer);
            ClearChildren(slotsContainer);
            _slots.Clear();
            _blocks.Clear();

            for (int i = 0; i < question.correctOrder.Count; i++)
            {
                var slot = Instantiate(slotPrefab, slotsContainer);
                _slots.Add(slot);
            }

            var shuffledIndices = Enumerable.Range(0, question.codeBlocks.Count)
                .OrderBy(_ => UnityEngine.Random.value)
                .ToList();

            foreach (int blockIndex in shuffledIndices)
            {
                var block = Instantiate(blockPrefab, blocksContainer);
                block.Initialize(blockIndex, question.codeBlocks[blockIndex], blocksContainer);
                _blocks.Add(block);
            }

            checkButton.interactable = true;
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(CheckAnswer);
        }

        private void CheckAnswer()
        {
            var currentOrder = new List<int>();
            foreach (var slot in _slots)
            {
                var block = slot.GetComponentInChildren<DragDropBlock>();
                currentOrder.Add(block != null ? block.BlockIndex : -1);
            }

            bool correct = currentOrder.SequenceEqual(_question.correctOrder);

            checkButton.interactable = false;
            foreach (var block in _blocks) block.SetDraggable(false);

            OnAnswered?.Invoke(correct);
        }

        private void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }

        public void Clear()
        {
            checkButton.onClick.RemoveAllListeners();
        }
    }
}
