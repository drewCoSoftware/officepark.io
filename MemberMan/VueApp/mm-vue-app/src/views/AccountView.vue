<script setup lang="ts">
import { useLoginStore } from '@/stores/login';
import  { onMounted } from 'vue';
import { useRouter } from 'vue-router';


const _Login = useLoginStore();
const _Router = useRouter();

onMounted(() => {
//  alert('account page is mounted!');


  if (!_Login.IsLoggedIn) {
    // OPTIONS: Configure the login page....
//    alert('we are not logged in!');
//    _Router.push("/?goto=/account");
  }

});


</script>

<template>
  <p>Hello, I am the account page!</p>
  <p>All kinds of features for users and their accounts may be used here!</p>
</template>