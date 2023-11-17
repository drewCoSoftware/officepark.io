<script setup lang="ts">
// EZForm lets us define a standard form wrapper that we can put any old content into.
// The main point is to have consistent ways to handle common tasks like working state,
// validation, error messages, etc. 
import { stringify } from 'querystring';
import { popScopeId, ref, watch } from 'vue';


const props = defineProps({
  //  isWorking: Boolean,
  //  errorMessage: String,
  cssClasses: String,
});

const TemplateClass = ref("ez-form");
const IsWorking = ref(false);
const ErrorMessage = ref<string | null>(null);

// We can expose component functions!  Yay!
function beginWork() {
  if (!IsWorking.value) {
    IsWorking.value = true;
    ClearErrors();
  }
}

function endWork() {
  IsWorking.value = false;
}

function SetErrorMessage(msg: string) {
  ErrorMessage.value = msg;
}

function ClearErrors() {
  ErrorMessage.value = "";
}

defineExpose({
  IsWorking,
  beginWork,
  endWork,
  SetErrorMessage,
  ClearErrors
});

watch([props, IsWorking, ErrorMessage], (x) => {
  updateTemplateClass();
});

const emit = defineEmits(['validate']);



// TOOD: Share this
function isNullOrEmpty(input: string | undefined) {
  const res = input == null || input == undefined || input == "";
  return res;
}

function updateTemplateClass() {

  let useVal = "ez-form";
  if (!isNullOrEmpty(props.cssClasses)) {
    useVal += " " + props.cssClasses
  }
  if (IsWorking.value) {
    useVal += " is-working";
  }
  if (ErrorMessage.value != null && ErrorMessage.value != "") {
    useVal += " has-error";
  }

  TemplateClass.value = useVal;
}


function onInputEvent() {
  // NOTE: This is kind of hacky since custom events in VUE don't bubble, which is totally stupid.
  // Either way, we need to determine what thing got an input, and how we might determine
  // if all of the inputs are valid.
  // That is total overkill at this time, and we can instead just write per-form
  // validation code and keep this project moving....
  // emit('validate');
}

</script>


<template>
  <div :class="TemplateClass" @input="onInputEvent">
    <div class="messages" v-html="ErrorMessage"></div>
    <form>
      <slot></slot>
    </form>
  </div>
</template>


<style scoped type="less">
.ez-form.is-working {
  background: red;
}
</style>
