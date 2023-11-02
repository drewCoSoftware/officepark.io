<script setup lang="ts">
// A simple way to add inputs to an EZForm.

import { ref, watch, computed} from 'vue';


// NOTE: The 'modelValue' and 'update:modelValue' bits are what allow us to use v-model 
// when using the EZInput component.
// See this: https://vuejs.org/guide/components/v-model.html for more details.
const props = defineProps(['modelValue', 'type', 'name', 'value', 'placeholder']);
const emit = defineEmits(['update:modelValue']);

function onInputChanged(e:Event) {
  emit('update:modelValue', (e.target as HTMLInputElement).value);
}

// function handleInput() {
//   alert('wwfwf');
// }

</script>


<template>
  <div class="ez-input">
    <textarea v-if="type == 'textarea'"></textarea>
    <input v-else :input="type" :name="name" 
           :placeholder="placeholder"
           :value="modelValue" @input="onInputChanged" />
  </div>
</template>