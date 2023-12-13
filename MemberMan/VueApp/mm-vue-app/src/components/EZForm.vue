<script setup lang="ts">
// EZForm lets us define a standard form wrapper that we can put any old content into.
// The main point is to have consistent ways to handle common tasks like working state,
// validation, error messages, etc. 
import { isNullOrEmpty } from '@/shared';
import { stringify } from 'querystring';
import { onMounted, popScopeId, ref, watch } from 'vue';
import { useSlots } from 'vue';


// Thanks Internet!
// https://stackoverflow.com/questions/55905055/vue-need-to-disable-all-inputs-on-page
// This is how we are disabling all of the inputs on a form when it is working.
// It may make more sense to just shove all of this into the EZFORM component....
// TODO: Put this into some kind of reusable file/package....
const vDisableInputs = {
  // When all the children of the parent component have been updated
  updated: function (el:any, binding:any) {

    // NOTE: This seems to fire a lot....
    const flag = binding.value;

    // NOTE:  If this is EZForm specific
    const tags = ["input", "button", "textarea", "select"];
    tags.forEach(tagName => {
      const nodes = el.getElementsByTagName(tagName);
      for (let i = 0; i < nodes.length; i++) {
        nodes[i].disabled = flag;
      }
    });
  }
};

const vEnableSubmit = {
  updated: function (el:any, binding:any) {
    const flag = binding.value;

    const selector = 'button[data-is-submit="true"],button[type="submit"]';
    const nodes = (el.querySelectorAll(selector));
    for (let i = 0; i < nodes.length; i++) {
      const node = nodes[i];
      node.disabled = !flag;
    }

  }
};



const slots = useSlots();

const props = defineProps({
  cssClasses: { type: String, default:''},
  validate: { type: Function, default: null },
});

const TemplateClass = ref("ez-form");
const IsWorking = ref(false);
const ErrorMessage = ref<string | null>(null);
const IsFormValid = ref(true);

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

watch([props, IsWorking, ErrorMessage, IsFormValid], (x) => {
  updateTemplateClass();
});

onMounted(() => {
    onInputEvent();
});

// ------------------------------------------------------------------------------------
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


// ------------------------------------------------------------------------------------
function onInputEvent() {

  if (props.validate != null) {
    const isValid = props.validate();
    IsFormValid.value = isValid;
  }
  else {
    IsFormValid.value = true;
  }
}




</script>


<template>
  <div :class="TemplateClass" @input="onInputEvent">
    <div class="shade">
      <img src="/src/assets/refresh.svg" />
    </div>
    <div class="messages" v-html="ErrorMessage"></div>
    <form v-disable-inputs="IsWorking" v-enable-submit="IsFormValid">
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
