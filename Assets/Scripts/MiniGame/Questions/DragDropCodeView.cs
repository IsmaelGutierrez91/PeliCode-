using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PeliCode.Data;
using PeliCode.Localization;
using PeliCode.UI;
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
            if (_reveal != null) { StopCoroutine(_reveal); _reveal = null; }
            questionText.text = CodeText.Plain(question.QuestionTextFor(Loc.Current));

            // Se destruyen los bloques de la pregunta anterior esten donde esten
            // (lista, casilla o a mitad de un arrastre), y despues las casillas.
            foreach (var block in _blocks)
                if (block != null) DestroyNow(block.gameObject);
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

            Lock();
            OnAnswered?.Invoke(correct);
        }

        private void Lock()
        {
            checkButton.interactable = false;
            foreach (var block in _blocks)
                if (block != null) block.SetDraggable(false);
        }

        private static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                DestroyNow(parent.GetChild(i).gameObject);
        }

        // Se saca del padre antes de destruir: Destroy es diferido y, mientras tanto,
        // el GridLayoutGroup acomodaria los bloques viejos junto a los nuevos.
        private static void DestroyNow(GameObject go)
        {
            go.transform.SetParent(null, false);
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        public void Clear()
        {
            Lock();
            checkButton.onClick.RemoveAllListeners();
        }

        /// <summary>Segundos con los bloques marcados antes de que se acomoden en su lugar.</summary>
        public const float MarkSeconds = 2f;
        /// <summary>Segundos que se deja ver el codigo ya ordenado para entender el error.</summary>
        public const float StudySeconds = 4.5f;
        private const float MoveSeconds = 0.4f;

        public float MistakeDelaySeconds => MarkSeconds + MoveSeconds + StudySeconds;

        private Coroutine _reveal;

        /// <summary>
        /// Marca en verde los bloques que estaban en su casilla correcta y en rojo los demas;
        /// a los 2 segundos todos se desplazan a su lugar y quedan en verde un rato para estudiarlos.
        /// </summary>
        public string RevealAnswer()
        {
            if (_question == null) return null;

            // Lo que habia puesto el jugador: que bloque esta en cada casilla (null = vacia)
            var placed = new DragDropBlock[_slots.Count];
            for (int i = 0; i < _slots.Count; i++)
                placed[i] = _slots[i].GetComponentInChildren<DragDropBlock>();

            foreach (var block in _blocks)
            {
                if (block == null) continue;
                int slot = Array.IndexOf(placed, block);
                bool right = slot >= 0 && slot < _question.correctOrder.Count && _question.correctOrder[slot] == block.BlockIndex;
                block.Mark(right ? UITheme.Success : UITheme.Error);
            }

            if (_reveal != null) StopCoroutine(_reveal);
            if (isActiveAndEnabled) _reveal = StartCoroutine(MoveToPlace());
            else PlaceInOrder(animate: false);
            return null;
        }

        private IEnumerator MoveToPlace()
        {
            yield return new WaitForSecondsRealtime(MarkSeconds);
            PlaceInOrder(animate: true);
        }

        /// <summary>Acomoda cada bloque en su casilla correcta (con un desplazamiento corto si animate).</summary>
        private void PlaceInOrder(bool animate)
        {
            foreach (var block in _blocks)
                if (block != null) block.ReturnToOrigin();

            for (int i = 0; i < _slots.Count && i < _question.correctOrder.Count; i++)
            {
                var block = _blocks.FirstOrDefault(b => b != null && b.BlockIndex == _question.correctOrder[i]);
                if (block == null) continue;
                Vector3 from = block.transform.position;
                block.transform.SetParent(_slots[i].transform, false);
                block.transform.localPosition = Vector3.zero;
                block.Mark(UITheme.Success);
                if (animate) StartCoroutine(Slide(block.transform, from));
            }
        }

        private static IEnumerator Slide(Transform block, Vector3 from)
        {
            Vector3 to = block.position;
            for (float t = 0f; t < MoveSeconds; t += Time.unscaledDeltaTime)
            {
                if (block == null) yield break;
                block.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / MoveSeconds));
                yield return null;
            }
            if (block != null) block.position = to;
        }
    }
}
