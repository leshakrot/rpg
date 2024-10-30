using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RPG.Dialogue
{
    public class PlayerConversant : MonoBehaviour
    {
        Dialogue _currentDialogue;
        private DialogueNode _currentNode = null;
        private bool _isChoosing = false;

        public event Action onConversationUpdated;

        public void StartDialogue(Dialogue newDialogue)
        {
            _currentDialogue = newDialogue;
            _currentNode = _currentDialogue.GetRootNode();
            onConversationUpdated();
        }

        public void Quit()
        {
            _currentDialogue = null;
            _currentNode = null;
            _isChoosing = false;
            onConversationUpdated();
        }

        public bool IsActive()
        {
            return _currentDialogue != null;
        }

        public bool IsChoosing()
        {
            return _isChoosing;
        }

        public string GetText()
        {
            if(_currentNode == null)
            {
                return "";
            }

            return _currentNode.GetText();
        }

        public IEnumerable<DialogueNode> GetChoices()
        {
            return _currentDialogue.GetPlayerChildren(_currentNode);
        }

        public void SelectChoice(DialogueNode chosenNode)
        {
            _currentNode = chosenNode;
            _isChoosing = false;
            Next();
        }

        public void Next()
        {
            int numPlayerResponses = _currentDialogue.GetPlayerChildren(_currentNode).Count();
            if(numPlayerResponses > 0)
            {
                _isChoosing = true;
                onConversationUpdated();
                return;
            }

            DialogueNode[] children = _currentDialogue.GetAIChildren(_currentNode).ToArray();
            int randomIndex = UnityEngine.Random.Range(0, children.Count());
            _currentNode = children[randomIndex];
            onConversationUpdated();
        }

        public bool HasNext()
        {
            return _currentDialogue.GetAllChildren(_currentNode).Count() > 0;
        }
    }
}
