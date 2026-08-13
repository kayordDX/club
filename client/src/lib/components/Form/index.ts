import Form from "./Form.svelte";
import FieldError from "./FieldError.svelte";
import { createFormCreator, createFormCreatorContexts } from "@tanstack/svelte-form";
import Input from "./Input.svelte";
import Submit from "./Submit.svelte";
import Select from "./Select.svelte";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
const isInvalid = (field: any) => {
	return field.state.meta.isTouched && !field.state.meta.isValid;
};

const { createAppForm } = createFormCreator({
	fieldComponents: {
		Input,
		Select,
	},
	formComponents: {
		Submit,
	},
});

const { useFieldContext, useFormContext } = createFormCreatorContexts();

export { Form, FieldError, isInvalid, createAppForm, useFieldContext, useFormContext };
