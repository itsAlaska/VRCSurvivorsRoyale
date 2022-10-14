﻿using System;
using System.Reflection;
using UnityEngine;
using VRC.Udon.Graph;

namespace Cyan.CT.Editor
{
    public class CyanTriggerCustomNodeSetComponentActive : 
        CyanTriggerCustomUdonActionNodeDefinition, 
        ICyanTriggerCustomNodeCustomVariableSettings,
        ICyanTriggerCustomNodeCustomHash,
        ICyanTriggerCustomNodeValidator
    {
        public const string FullName =
            "UnityEngineGameObject.__SetComponentActive__UnityEngineGameObject__SystemString__SystemBoolean";
        
        public static readonly UdonNodeDefinition NodeDefinition = new UdonNodeDefinition(
            "GameObject SetComponentActive",
            FullName,
            typeof(GameObject),
            new[]
            {
                new UdonNodeParameter()
                {
                    name = "instance", 
                    type = typeof(GameObject),
                    parameterType = UdonNodeParameter.ParameterType.IN
                },
                new UdonNodeParameter()
                {
                    name = "Component Name", 
                    type = typeof(string),
                    parameterType = UdonNodeParameter.ParameterType.IN
                },
                new UdonNodeParameter()
                {
                    name = "value", 
                    type = typeof(bool),
                    parameterType = UdonNodeParameter.ParameterType.IN
                }
            },
            Array.Empty<string>(),
            Array.Empty<string>(),
            Array.Empty<object>(),
            true
        );

        public static readonly CyanTriggerActionVariableDefinition[] VariableDefinitions =
        {
            new CyanTriggerActionVariableDefinition
            {
                type = new CyanTriggerSerializableType(typeof(GameObject)),
                udonName = "instance",
                displayName = "GameObject", 
                variableType = CyanTriggerActionVariableTypeDefinition.Constant | 
                               CyanTriggerActionVariableTypeDefinition.AllowsMultiple |
                               CyanTriggerActionVariableTypeDefinition.VariableInput
            },
            new CyanTriggerActionVariableDefinition
            {
                type = new CyanTriggerSerializableType(typeof(string)),
                udonName = "Component Name",
                displayName = "Component Name", 
                variableType = CyanTriggerActionVariableTypeDefinition.Constant
            },
            new CyanTriggerActionVariableDefinition
            {
                type = new CyanTriggerSerializableType(typeof(bool)),
                udonName = "value",
                displayName = "Value", 
                variableType = CyanTriggerActionVariableTypeDefinition.Constant | 
                               CyanTriggerActionVariableTypeDefinition.VariableInput
            }
        };
        
        
        public override UdonNodeDefinition GetNodeDefinition()
        {
            return NodeDefinition;
        }
        
        public CyanTriggerActionVariableDefinition[] GetCustomVariableSettings()
        {
            return VariableDefinitions;
        }

        public override CyanTriggerNodeDefinition.UdonDefinitionType GetDefinitionType()
        {
            return CyanTriggerNodeDefinition.UdonDefinitionType.Method;
        }

        public override string GetDisplayName()
        {
            return "SetComponentActive";
        }
        
        public override string GetDocumentationLink()
        {
            return CyanTriggerDocumentationLinks.SetComponentActiveNodeDocumentation;
        }

        public string GetCustomHash(CyanTriggerActionInstance actionInstance)
        {
            return GetCustomHashForComponent(actionInstance);
        }
        
        public static string GetCustomHashForComponent(CyanTriggerActionInstance actionInstance)
        {
            return $"ComponentName: {actionInstance.inputs[1].data.Obj}";
        }

        public CyanTriggerErrorType Validate(
            CyanTriggerActionInstance actionInstance, 
            CyanTriggerDataInstance triggerData, 
            ref string message)
        {
            return ValidateComponent(actionInstance.inputs[1].data.Obj, ref message);
        }

        public static CyanTriggerErrorType ValidateComponent(object componentNameData, ref string message)
        {
            if (!(componentNameData is string compName))
            {
                message = $"Invalid Component: {componentNameData}";
                return CyanTriggerErrorType.Error;
            }

            if (!CyanTriggerNodeDefinitionManager.Instance.TryGetComponentType(compName, out Type type))
            {
                message = $"Invalid Component: {compName}";
                return CyanTriggerErrorType.Error;
            }
            
            PropertyInfo enabledProperty = type.GetProperty(nameof(Behaviour.enabled));
            if (enabledProperty == null)
            {
                message = "Component cannot be enabled";
                return CyanTriggerErrorType.Error;
            }

            return CyanTriggerErrorType.None;
        }

        public override void AddActionToProgram(CyanTriggerCompileState compileState)
        {
            var actionInstance = compileState.ActionInstance;
            var actionMethod = compileState.ActionMethod;

            // This value is not normally available at compile time. The hash method in CyanTriggerInstanceDataHash is
            // required to include this value in the hash...
            string componentTypeString = actionInstance.inputs[1].data?.Obj as string;
            
            if (string.IsNullOrEmpty(componentTypeString))
            {
                return;
            }

            var data = compileState.Program.Data;
            
            if (!CyanTriggerNodeDefinitionManager.Instance.TryGetComponentType(componentTypeString, out var componentType))
            {
                Debug.LogError($"Cannot find type to change component active state: {componentTypeString}");
                return;
            }
            
            PropertyInfo enabledProperty = componentType.GetProperty(nameof(Behaviour.enabled));
            if (enabledProperty == null)
            {
                Debug.LogError($"Cannot change enabled property of type {componentTypeString}");
                return;
            }

            Type componentArrayType = typeof(Component[]);
            
            /*
             type = getType(stringType)
             cond = isValid(type)
             jump to end 
             
             <generate for all multi-inputs>
            temporary array of type
            array = gameobject.getcomponents(type)
            temp int index = 0
            temp int len = array.length
            jump begin nop
            temp bool = index < len
            jump if false nop jump end
            type c = array[index]
            c.enabled = bool
            index = 1 + index
            jump to begin nop
            nop jump end
             */
            
            
            CyanTriggerAssemblyInstruction jumpEndFinalNop = CyanTriggerAssemblyInstruction.Nop();
            
            var typeVariable =
                compileState.GetDataFromVariableInstance(-1, 1, actionInstance.inputs[1], typeof(string), false);
            var boolVariable =
                compileState.GetDataFromVariableInstance(-1, 2, actionInstance.inputs[2], typeof(bool), false);
            
            CyanTriggerAssemblyDataType tempType = data.RequestTempVariable(typeof(Type));
            CyanTriggerAssemblyDataType tempComponent = data.RequestTempVariable(componentType);
            CyanTriggerAssemblyDataType tempArray = data.RequestTempVariable(componentArrayType);
            CyanTriggerAssemblyDataType tempIndex = data.RequestTempVariable(typeof(int));
            CyanTriggerAssemblyDataType tempLength = data.RequestTempVariable(typeof(int));
            CyanTriggerAssemblyDataType tempCondition = data.RequestTempVariable(typeof(bool));

            CyanTriggerAssemblyInstruction pushTypeString = CyanTriggerAssemblyInstruction.PushVariable(typeVariable);
            CyanTriggerAssemblyInstruction pushType = CyanTriggerAssemblyInstruction.PushVariable(tempType);
            CyanTriggerAssemblyInstruction pushComp = CyanTriggerAssemblyInstruction.PushVariable(tempComponent);
            CyanTriggerAssemblyInstruction pushArray = CyanTriggerAssemblyInstruction.PushVariable(tempArray);
            CyanTriggerAssemblyInstruction pushIndex = CyanTriggerAssemblyInstruction.PushVariable(tempIndex);
            CyanTriggerAssemblyInstruction pushLength = CyanTriggerAssemblyInstruction.PushVariable(tempLength);
            CyanTriggerAssemblyInstruction pushCondition =
                CyanTriggerAssemblyInstruction.PushVariable(tempCondition);
            
            

            var setEnabledExtern = CyanTriggerAssemblyInstruction.CreateExtern(
                CyanTriggerDefinitionResolver.GetMethodSignature(enabledProperty.GetSetMethod()));
            var getComponentsMethod = typeof(GameObject).GetMethod(nameof(GameObject.GetComponents),
                BindingFlags.Public | BindingFlags.Instance, null, new[] {typeof(Type)}, null);
            var getComponentExtern = CyanTriggerAssemblyInstruction.CreateExtern(
                CyanTriggerDefinitionResolver.GetMethodSignature(getComponentsMethod));
            var intLessExtern = CyanTriggerAssemblyInstruction.CreateExtern(
                CyanTriggerDefinitionResolver.GetPrimitiveOperationSignature(typeof(int),
                    PrimitiveOperation.LessThan));
            var intAddExtern = CyanTriggerAssemblyInstruction.CreateExtern(
                CyanTriggerDefinitionResolver.GetPrimitiveOperationSignature(typeof(int),
                    PrimitiveOperation.Addition));
            var arrayLenExtern = CyanTriggerAssemblyInstruction.CreateExtern(
                CyanTriggerDefinitionResolver.GetMethodSignature(componentArrayType
                    .GetProperty(nameof(Array.Length))
                    .GetGetMethod()));
            var arrayGetExtern = CyanTriggerAssemblyInstruction.CreateExtern(
                CyanTriggerDefinitionResolver.GetMethodSignature(componentArrayType.GetMethod("Get")));
            

            var int0Const = data.GetOrCreateVariableConstant(typeof(int), 0);
            var int1Const = data.GetOrCreateVariableConstant(typeof(int), 1);
            
            
            // get type from string input
            actionMethod.AddAction(pushTypeString);
            actionMethod.AddAction(pushType);
            actionMethod.AddAction(CyanTriggerAssemblyInstruction.CreateExtern(
                CyanTriggerDefinitionResolver.GetMethodSignature(
                    typeof(Type).GetMethod(nameof(Type.GetType), new [] {typeof (string)}))));

            // IsValid(type)
            actionMethod.AddAction(pushType);
            actionMethod.AddAction(pushCondition);
            actionMethod.AddAction(CyanTriggerAssemblyInstruction.CreateExtern(
                CyanTriggerDefinitionResolver.GetMethodSignature(
                    typeof(VRC.SDKBase.Utilities).GetMethod(nameof(VRC.SDKBase.Utilities.IsValid), BindingFlags.Static | BindingFlags.Public))));
            
            // jump if !cond
            actionMethod.AddAction(pushCondition);
            actionMethod.AddAction(CyanTriggerAssemblyInstruction.JumpIfFalse(jumpEndFinalNop));
            
            
            for (int curInput = 0; curInput < actionInstance.multiInput.Length; ++curInput)
            {
                CyanTriggerAssemblyInstruction jumpStartNop = CyanTriggerAssemblyInstruction.Nop();
                CyanTriggerAssemblyInstruction jumpEndNop = CyanTriggerAssemblyInstruction.Nop();

                var gameObjectVariable =
                    compileState.GetDataFromVariableInstance(curInput, 0, actionInstance.multiInput[curInput], typeof(GameObject),
                        false);
                
                // array = gameobject.GetComponents(type);
                actionMethod.AddAction(CyanTriggerAssemblyInstruction.PushVariable(gameObjectVariable));
                actionMethod.AddAction(pushType);
                actionMethod.AddAction(pushArray);
                actionMethod.AddAction(getComponentExtern);

                // index = 0;
                actionMethod.AddAction(CyanTriggerAssemblyInstruction.PushVariable(int0Const));
                actionMethod.AddAction(pushIndex);
                actionMethod.AddAction(CyanTriggerAssemblyInstruction.Copy());

                // length = array.length;
                actionMethod.AddAction(pushArray);
                actionMethod.AddAction(pushLength);
                actionMethod.AddAction(arrayLenExtern);

                actionMethod.AddAction(jumpStartNop);

                // cond = index < length
                actionMethod.AddAction(pushIndex);
                actionMethod.AddAction(pushLength);
                actionMethod.AddAction(pushCondition);
                actionMethod.AddAction(intLessExtern);

                // jump if !cond
                actionMethod.AddAction(pushCondition);
                actionMethod.AddAction(CyanTriggerAssemblyInstruction.JumpIfFalse(jumpEndNop));

                // type c = array[index]
                actionMethod.AddAction(pushArray);
                actionMethod.AddAction(pushIndex);
                actionMethod.AddAction(pushComp);
                actionMethod.AddAction(arrayGetExtern);

                // c.enabled = <bool Input>
                actionMethod.AddAction(pushComp);
                actionMethod.AddAction(CyanTriggerAssemblyInstruction.PushVariable(boolVariable));
                actionMethod.AddAction(setEnabledExtern);

                // index = 1 + index
                actionMethod.AddAction(pushIndex);
                actionMethod.AddAction(CyanTriggerAssemblyInstruction.PushVariable(int1Const));
                actionMethod.AddAction(pushIndex);
                actionMethod.AddAction(intAddExtern);

                // Jump back to condition
                actionMethod.AddAction(CyanTriggerAssemblyInstruction.Jump(jumpStartNop));
                actionMethod.AddAction(jumpEndNop);
            }
            
            actionMethod.AddAction(jumpEndFinalNop);
            
            data.ReleaseTempVariable(tempType);
            data.ReleaseTempVariable(tempComponent);
            data.ReleaseTempVariable(tempArray);
            data.ReleaseTempVariable(tempIndex);
            data.ReleaseTempVariable(tempLength);
            data.ReleaseTempVariable(tempCondition);
        }
    }
}
