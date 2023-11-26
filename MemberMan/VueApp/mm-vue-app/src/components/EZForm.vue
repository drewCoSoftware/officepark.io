<script setup lang="ts">
// EZForm lets us define a standard form wrapper that we can put any old content into.
// The main point is to have consistent ways to handle common tasks like working state,
// validation, error messages, etc. 
import { stringify } from 'querystring';
import { onMounted, popScopeId, ref, watch } from 'vue';
import { useSlots } from 'vue';

const slots = useSlots();

// // TEMP:
// onMounted(() => {
//   // I understand slots better now, but I still have no idea how I can actually
//   // utilize them.  VUE does a lot of nice things for us, but completely hiding
//   // the dom and relationship between parts is really annoying....

//   // const kidSlot = slots.default;
//   // console.log('kids slot is:');
//   // console.log(kidSlot);

// });



const props = defineProps({
  cssClasses: { type: String, default:''},
  enableSubmit: {type: Boolean, default: true }
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



// TOOD: Share this function somewhere.....
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
    <div class="shade">
      <img src="/src/assets/refresh.svg" />
    </div>
    <div class="messages" v-html="ErrorMessage"></div>
    <form v-disable-inputs="IsWorking" v-enable-submit="props.enableSubmit">
      <slot />
    </form>
  </div>
</template>


<style scoped type="less">
@keyframes loadSpin {
  from {
    transform: rotate(0deg);
  }

  to {
    transform: rotate(180deg);
  }
}


.ez-form {
  padding: 1rem;
  position: relative;

  .messages {
    min-height: 1.5rem;
    color: red;
    opacity: 0;
    transition: all linear 0.125s;
    margin-bottom: 0.5rem;
  }


  .shade {
    display:none;
    position: absolute;
    background: #FFFFFF5A;
    z-index: 999;
    width: 100%;
    height: 100%;
    line-height: 1;

    justify-content: center;
    align-items: center;
    img {
      height: 50px;
      animation-name: loadSpin;
      animation-duration: 0.75s;
      animation-iteration-count: infinite;
      animation-timing-function: cubic-bezier();
    }
  }
}

.ez-form.has-error {
  .messages {
    opacity: 1;
  }
}

.ez-form.is-working {
  .shade {
    display: flex;
  }
}
</style>
