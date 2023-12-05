

export function isNullOrEmpty(input: string | undefined) {
  const res = input == null || input == undefined || input == "";
  return res;
}
