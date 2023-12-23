<script setup lang="ts">
import { useLoginStore } from '@/stores/mmlogin';
import  { onMounted } from 'vue';
import { useRouter } from 'vue-router';


const _Login = useLoginStore();
const _Router = useRouter();

onMounted(() => {
  if (!_Login.IsLoggedIn) {
    _Router.push("/login?goto=/account");
  }
});


</script>

<template>
  <p>Hello, I am the account page!</p>
  <p>All kinds of features for users and their accounts may be used here!</p>
</template>