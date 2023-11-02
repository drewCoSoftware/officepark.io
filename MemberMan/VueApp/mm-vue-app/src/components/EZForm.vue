<script setup lang="ts">
// EZForm lets us define a standard form wrapper that we can put any old content into.
// The main point is to have consistent ways to handle common tasks like working state,
// validation, error messages, etc. 
import { stringify } from 'querystring';
import { ref, watch } from 'vue';


const props = defineProps({
  isWorking: Boolean,
  errorMessage: String,
  cssClasses:String
});

const emit = defineEmits(['validate']);

const templateClass = ref("ez-form");

// TOOD: Share this
function isNullOrEmpty(input:string | undefined) {
  const res = input == null || input == undefined || input == "";
  return res;
}

function updateTemplateClass() {

  let useVal = "ez-form";
  if (!isNullOrEmpty(props.cssClasses)) {
    useVal += " " + props.cssClasses
  }
  if (props.isWorking) { 
    useVal += " is-working";
  }
  if (props.errorMessage != null && props.errorMessage != ""){
    useVal += " has-error";
  }

  templateClass.value = useVal;
}

watch(props, (x) => {
  updateTemplateClass();
});

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
  <div :class="templateClass" @input="onInputEvent">
    <div class="messages" v-html="errorMessage"></div>
    <form>
      <slot ></slot>
    </form>
  </div>

</template>
