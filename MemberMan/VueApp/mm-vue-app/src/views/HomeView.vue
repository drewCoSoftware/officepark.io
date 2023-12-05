<script setup lang="ts">
import { useRouter } from 'vue-router';

</script>

<template>
  <h2>Welcome</h2>
  <p>This is an example VUE application that can interface with a MemberMan back-end.</p>
  <p>MemberMan is a standlone backend that can handle all of your login + basic account management needs. It is designed
    to be extensible so you can tailor it to your solution and environment as needed.</p>

  <p>If you have an account, you may <RouterLink to="/login">log in</RouterLink>, otherwise you may <RouterLink to="/signup">sign up</RouterLink> for a new one.</p>



</template>